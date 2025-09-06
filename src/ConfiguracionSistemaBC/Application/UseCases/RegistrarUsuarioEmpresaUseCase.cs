using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// Domain
using ConfiguracionSistemaBC.Domain.Aggregates;           // UsuarioEmpresa, UsuarioEmpresaEstado, RolEmpresa
using ConfiguracionSistemaBC.Domain.Repositories;         // IUsuarioEmpresaRepository, IRolEmpresaRepository, IConfiguracionEmpresaRepository, IUnitOfWork

// Shared Kernel (VOs / Contexto)
using SharedKernel.Application.Interfaces;                // ITenantContext
using SharedKernel.ValueObjects;                          // EmpresaId, UsuarioId, EstablecimientoId, Email, Telefono, NombrePersona

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Registra la membresía de un usuario dentro de una empresa (multiempresa).
    /// - Valida unicidad de email en la empresa.
    /// - Valida existencia de establecimientos en la empresa.
    /// - Valida que los roles existan (del sistema o personalizados de esta empresa).
    /// - Crea el agregado UsuarioEmpresa en estado Invitado con accesos/roles iniciales.
    /// </summary>
    public sealed class RegistrarUsuarioEmpresaUseCase
    {
        private readonly ITenantContext? _tenant;
        private readonly IUsuarioEmpresaRepository _usuarioRepo;
        private readonly IConfiguracionEmpresaRepository _configRepo;
        private readonly IRolEmpresaRepository _rolRepo;
        private readonly IUnitOfWork _uow;

        public RegistrarUsuarioEmpresaUseCase(
            IUsuarioEmpresaRepository usuarioRepo,
            IConfiguracionEmpresaRepository configRepo,
            IRolEmpresaRepository rolRepo,
            IUnitOfWork uow,
            ITenantContext? tenantContext = null)
        {
            _usuarioRepo = usuarioRepo ?? throw new ArgumentNullException(nameof(usuarioRepo));
            _configRepo  = configRepo  ?? throw new ArgumentNullException(nameof(configRepo));
            _rolRepo     = rolRepo     ?? throw new ArgumentNullException(nameof(rolRepo));
            _uow         = uow         ?? throw new ArgumentNullException(nameof(uow));
            _tenant      = tenantContext; // puede ser null en escenarios backoffice; en ese caso se exige EmpresaId en DTO
        }

        public async Task<RegistrarUsuarioEmpresaOutputDto> HandleAsync(
            RegistrarUsuarioEmpresaInputDto input,
            CancellationToken ct = default)
        {

            if (input is null) throw new ArgumentNullException(nameof(input));
            if (input.Nombres is null) throw new ArgumentNullException(nameof(input.Nombres));
            if (input.Apellidos is null) throw new ArgumentNullException(nameof(input.Apellidos));
            if (input.Email is null) throw new ArgumentNullException(nameof(input.Email));

            // 1) Resolver EmpresaId (siempre del contexto de tenant)
            if (_tenant?.EmpresaId is null)
                throw new InvalidOperationException("No hay contexto de empresa activo. El usuario no debe ingresar ni seleccionar RUC.");
            var empresaId = _tenant.EmpresaId;

            // 2) Mapear VO básicos (convertir primitivos a value objects)
            var nombre = NombrePersona.Crear(input.Nombres, input.Apellidos);
            var email = Email.Create(input.Email);
            var telefono = string.IsNullOrWhiteSpace(input.Telefono) ? null : Telefono.FromTexto(input.Telefono);
            DocumentoIdentidad? documento = null; // Ya no se recibe documento desde el input DTO

            // 3) Reglas de negocio / Validaciones de existencia

            if (await _usuarioRepo.EmailExisteEnEmpresaAsync(empresaId, email, ct))
                throw new InvalidOperationException("Ya existe un usuario con ese email en la empresa.");

            if (input.AccesosPorEstablecimiento is null || input.AccesosPorEstablecimiento.Count == 0)
                throw new InvalidOperationException("Debe asignar al menos un establecimiento con roles.");

            // Valida establecimientos y construye tuplas de accesos (VOs)
            var accesosTuplas = new List<(EstablecimientoId EstablecimientoId, IEnumerable<Guid> RolIds)>();
            var estIdsVistos = new HashSet<EstablecimientoId>();

            foreach (var a in input.AccesosPorEstablecimiento)
            {
                if (a is null) continue;
                var estId = EstablecimientoId.From(a.EstablecimientoId);
                if (!estIdsVistos.Add(estId))
                    throw new InvalidOperationException("No puede repetir el mismo establecimiento en los accesos.");

                var existe = await _configRepo.EstablecimientoExisteAsync(empresaId, estId, ct);
                if (!existe)
                    throw new KeyNotFoundException("Establecimiento no encontrado en la empresa.");

                if (a.RolIds is null || a.RolIds.Count == 0)
                    throw new InvalidOperationException("Debe asignar al menos un rol en cada establecimiento.");

                // Validar roles del acceso
                foreach (var rolId in a.RolIds.Distinct())
                    await AsegurarRolValidoParaEmpresaAsync(rolId, empresaId, ct);

                accesosTuplas.Add((estId, a.RolIds.Distinct()));
            }

            // Roles de empresa (opcionales, se aplican a todos los establecimientos)
            var rolesEmpresaIds = (input.RolesEmpresaIds ?? new List<Guid>()).Distinct().ToArray();
            foreach (var rolId in rolesEmpresaIds)
                await AsegurarRolValidoParaEmpresaAsync(rolId, empresaId, ct);

            // 4) Crear el agregado en estado Invitado

            var usuarioId = UsuarioId.New();

            var agregado = UsuarioEmpresa.Crear(
                empresaId: empresaId,
                usuarioId: usuarioId,
                documento: documento, // opcional
                nombre: nombre,
                emailContacto: email,
                telefonoContacto: telefono,
                rolesEmpresaIds: rolesEmpresaIds,
                accesosIniciales: accesosTuplas
            );

            // 5) Persistencia
            await _usuarioRepo.AddAsync(agregado, ct);
            await _uow.SaveChangesAsync(ct);

            // 6) Salida
            return new RegistrarUsuarioEmpresaOutputDto
            {
                EmpresaId = Guid.TryParse(empresaId.Value, out var eid) ? eid : Guid.Empty,
                UsuarioId = usuarioId.Value,
                Estado = agregado.Estado.ToString(),
                Nombres = nombre.Nombres,
                Apellidos = nombre.Apellidos,
                Email = email.Value,
                Telefono = telefono?.UnirParaMostrar(),
                // TipoDocumento y NumeroDocumento eliminados del output según requerimiento
                Accesos = agregado.Accesos.Select(a => new RegistrarUsuarioEmpresaOutputDto.AccesoOut
                {
                    EstablecimientoId = a.EstablecimientoId.Value,
                    RolIds = a.RolIds.ToArray()
                }).ToList(),
                RolesEmpresaIds = agregado.RolesEmpresaIds.ToList()
            };
        }

        // -------------------- Helpers internos --------------------

        private static EmpresaId MapEmpresaIdFromDto(string? raw)
        {
            if (raw is null)
                throw new ArgumentNullException(nameof(raw), "EmpresaId es obligatorio si no hay contexto de tenant.");
            return EmpresaId.From(raw);
        }

        private async Task AsegurarRolValidoParaEmpresaAsync(Guid rolId, EmpresaId empresaId, CancellationToken ct)
        {
            var rol = await _rolRepo.GetByIdAsync(rolId, ct);
            if (rol is null)
                throw new KeyNotFoundException($"Rol no encontrado: {rolId}");

            // Permitido: roles del sistema (EmpresaId == null) o personalizados de ESTA empresa
            if (rol.EmpresaId is not null && !string.Equals(rol.EmpresaId.Value, empresaId.Value, StringComparison.Ordinal))
                throw new InvalidOperationException("El rol indicado pertenece a otra empresa.");
        }
    }
}

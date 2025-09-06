using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// Domain
using ConfiguracionSistemaBC.Domain.Aggregates;           // UsuarioEmpresa
using ConfiguracionSistemaBC.Domain.Repositories;         // IUsuarioEmpresaRepository, IRolEmpresaRepository, IConfiguracionEmpresaRepository, IUnitOfWork

// Shared Kernel (VOs / Contexto)
using SharedKernel.Application.Interfaces;                // ITenantContext
using SharedKernel.ValueObjects;                          // EmpresaId, UsuarioId, EstablecimientoId, Email, Telefono, NombrePersona

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Actualiza datos de perfil de un usuario dentro de la empresa (multiempresa).
    /// - Email NO cambia.
    /// - Puede actualizar nombres y teléfono.
    /// - Puede reemplazar roles de empresa.
    /// - Puede reemplazar accesos (establecimientos + roles).
    /// </summary>
    public sealed class ActualizarPerfilUsuarioEmpresaUseCase
    {
        private readonly ITenantContext _tenant;
        private readonly IUsuarioEmpresaRepository _usuarioRepo;
        private readonly IConfiguracionEmpresaRepository _configRepo;
        private readonly IRolEmpresaRepository _rolRepo;
        private readonly IUnitOfWork _uow;

        public ActualizarPerfilUsuarioEmpresaUseCase(
            ITenantContext tenantContext,
            IUsuarioEmpresaRepository usuarioRepo,
            IConfiguracionEmpresaRepository configRepo,
            IRolEmpresaRepository rolRepo,
            IUnitOfWork uow)
        {
            _tenant     = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
            _usuarioRepo = usuarioRepo  ?? throw new ArgumentNullException(nameof(usuarioRepo));
            _configRepo  = configRepo   ?? throw new ArgumentNullException(nameof(configRepo));
            _rolRepo     = rolRepo      ?? throw new ArgumentNullException(nameof(rolRepo));
            _uow         = uow          ?? throw new ArgumentNullException(nameof(uow));
        }

        public async Task<ActualizarPerfilUsuarioEmpresaOutputDto> HandleAsync(
            ActualizarPerfilUsuarioEmpresaInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (input.UsuarioId == Guid.Empty)
                throw new ArgumentNullException(nameof(input.UsuarioId));
            var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("EmpresaId no disponible en el contexto.");

            // 1) Cargar agregado
            var usuarioId = UsuarioId.From(input.UsuarioId);
            var agg = await _usuarioRepo.GetAsync(empresaId, usuarioId, ct);
            if (agg is null) throw new KeyNotFoundException("Usuario no encontrado en la empresa.");

            // 2) Actualizar datos de contacto (nombre/teléfono). Email NO cambia.
            bool quiereCambiarNombre = !string.IsNullOrWhiteSpace(input.Nombres) || !string.IsNullOrWhiteSpace(input.Apellidos);
            bool quiereCambiarTelefono = input.Telefono is not null;

            if (quiereCambiarNombre || quiereCambiarTelefono)
            {
                var nombres   = !string.IsNullOrWhiteSpace(input.Nombres)   ? input.Nombres!.Trim()   : agg.Nombre.Nombres;
                var apellidos = !string.IsNullOrWhiteSpace(input.Apellidos) ? input.Apellidos!.Trim() : agg.Nombre.Apellidos;

                var nombre = NombrePersona.Crear(nombres, apellidos);

                Telefono? telefono = agg.TelefonoContacto;
                if (input.Telefono is not null)
                {
                    var raw = input.Telefono.Trim();
                    telefono = string.IsNullOrWhiteSpace(raw) ? null : Telefono.FromTexto(raw);
                }

                // Email no cambia: se pasa el mismo
                agg.ActualizarDatosContacto(nombre, agg.EmailContacto, telefono);
            }

            // 3) Reemplazar roles de EMPRESA si se provee
            if (input.RolesEmpresaIds is not null)
            {
                var rolesEmpresa = input.RolesEmpresaIds
                    .Where(r => r != Guid.Empty)
                    .Distinct()
                    .ToArray();

                foreach (var rolId in rolesEmpresa)
                    await AsegurarRolValidoParaEmpresaAsync(rolId, empresaId, ct);

                agg.AsignarRolesEmpresa(rolesEmpresa);
            }

            // 4) Reemplazar ACCESOS por establecimiento si se provee
            if (input.AccesosPorEstablecimiento is not null)
            {
                if (input.AccesosPorEstablecimiento.Count == 0)
                    throw new InvalidOperationException("Debe asignar al menos un acceso si decide reemplazarlos.");

                // Validación y construcción
                var accesosTuplas = new List<(EstablecimientoId EstablecimientoId, IEnumerable<Guid> RolIds)>();
                var dupCheck = new HashSet<Guid>();

                foreach (var a in input.AccesosPorEstablecimiento)
                {
                    if (a is null) continue;
                    if (a.EstablecimientoId == Guid.Empty)
                        throw new ArgumentNullException(nameof(a.EstablecimientoId), "EstablecimientoId inválido.");

                    if (!dupCheck.Add(a.EstablecimientoId))
                        throw new InvalidOperationException("No puede repetir el mismo establecimiento en los accesos.");

                    var estId = EstablecimientoId.From(a.EstablecimientoId);

                    var existe = await _configRepo.EstablecimientoExisteAsync(empresaId, estId, ct);
                    if (!existe)
                        throw new KeyNotFoundException("Establecimiento no encontrado en la empresa.");

                    if (a.RolIds is null || a.RolIds.Count == 0)
                        throw new InvalidOperationException("Cada acceso debe tener al menos un rol.");

                    var rolesLimpios = a.RolIds.Where(x => x != Guid.Empty).Distinct().ToArray();
                    if (rolesLimpios.Length == 0)
                        throw new InvalidOperationException("RolIds inválidos.");

                    foreach (var rolId in rolesLimpios)
                        await AsegurarRolValidoParaEmpresaAsync(rolId, empresaId, ct);

                    accesosTuplas.Add((estId, rolesLimpios));
                }

                agg.ReemplazarAccesos(accesosTuplas);
            }

            // 5) Persistencia con concurrencia optimista
            await _usuarioRepo.UpdateAsync(agg, input.ExpectedVersion, ct);
            await _uow.SaveChangesAsync(ct);

            // 6) Salida
            return new ActualizarPerfilUsuarioEmpresaOutputDto
            {
                UsuarioId = agg.UsuarioId.Value,
                Estado = agg.Estado.ToString(),
                NombreCompleto = agg.Nombre.Completo,
                Email = agg.EmailContacto.Value,
                Telefono = agg.TelefonoContacto?.UnirParaMostrar() ?? string.Empty,
                RolesEmpresaIds = agg.RolesEmpresaIds.ToList(),
                Accesos = agg.Accesos.Select(a => new ActualizarPerfilUsuarioEmpresaOutputDto.AccesoOut
                {
                    EstablecimientoId = a.EstablecimientoId.Value,
                    RolIds = a.RolIds.ToList()
                }).ToList()
            };
        }

        // -------------------- Helpers internos --------------------

        private async Task AsegurarRolValidoParaEmpresaAsync(Guid rolId, EmpresaId empresaId, CancellationToken ct)
        {
            var rol = await _rolRepo.GetByIdAsync(rolId, ct);
            if (rol is null)
                throw new KeyNotFoundException($"Rol no encontrado: {rolId}");

            // Permitido: rol de sistema (EmpresaId == null) o rol personalizado de ESTA empresa
            if (rol.EmpresaId is not null && !string.Equals(rol.EmpresaId.Value, empresaId.Value, StringComparison.Ordinal))
                throw new InvalidOperationException("El rol indicado pertenece a otra empresa.");
        }
    }
}

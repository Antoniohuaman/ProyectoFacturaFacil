using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Application.DTOs;
using ConfiguracionSistemaBC.Domain.Aggregates;          // UsuarioEmpresa
using ConfiguracionSistemaBC.Domain.Repositories;        // IUsuarioEmpresaRepository
using SharedKernel.ValueObjects;                         // EmpresaId, UsuarioId, EstablecimientoId, Email, Telefono, NombrePersona
using SharedKernel.Application.Interfaces;               // ITenantContext
      

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso: Registrar un usuario dentro de la empresa (tenant actual).
    /// - Email obligatorio y único por empresa.
    /// - Nombre obligatorio, celular opcional.
    /// - Debe tener al menos un acceso con 1+ roles.
    /// - Crea la membresía en estado Invitado (habilitación es otro caso de uso).
    /// </summary>
    public sealed class RegistrarUsuarioEmpresaUseCase
    {
    private readonly ITenantContext _tenant;
    private readonly IUsuarioEmpresaRepository _repo;
    private readonly IRolEmpresaRepository _rolRepo;
    private readonly IUnitOfWork _uow;

        public RegistrarUsuarioEmpresaUseCase(
            ITenantContext tenant,
            IUsuarioEmpresaRepository repo,
            IRolEmpresaRepository rolRepo,
            IUnitOfWork uow)
        {
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _rolRepo = rolRepo ?? throw new ArgumentNullException(nameof(rolRepo));
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        }

        public async Task<RegistrarUsuarioEmpresaOutputDto> ExecuteAsync(
            RegistrarUsuarioEmpresaInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));

            // Validaciones de entrada
            if (string.IsNullOrWhiteSpace(input.Email))
                throw new ArgumentException("El email es obligatorio.", nameof(input.Email));

            if (input.Accesos is null || input.Accesos.Count == 0)
                throw new ArgumentException("Debes asignar al menos un establecimiento con roles.", nameof(input.Accesos));

            foreach (var a in input.Accesos)
            {
                if (a.EstablecimientoId == Guid.Empty)
                    throw new ArgumentException("EstablecimientoId inválido en Accesos.");
                if (a.RolIds is null || a.RolIds.Count == 0 || a.RolIds.Any(id => id == Guid.Empty))
                    throw new ArgumentException("Cada acceso debe contener al menos un RolId válido.");
            }

            var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("EmpresaId no disponible en el contexto.");
            var emailVO = Email.Create(input.Email.Trim());

            // Validación de roles
            foreach (var acceso in input.Accesos)
            {
                foreach (var rolId in acceso.RolIds)
                {
                    var rol = await _rolRepo.GetByIdAsync(rolId, ct);
                    if (rol == null)
                        throw new InvalidOperationException($"El rol con ID {rolId} no existe.");
                    if (!rol.EsSistema && rol.EmpresaId != empresaId)
                        throw new InvalidOperationException($"El rol con ID {rolId} no pertenece a la empresa actual.");
                }
            }

            // Regla de unicidad de email en la empresa
            var emailUsado = await _repo.EmailExisteEnEmpresaAsync(empresaId, emailVO, ct);
            if (emailUsado)
                throw new InvalidOperationException("El email ya existe en la empresa.");

            // (Opcionales) Nombre y teléfono
                if (string.IsNullOrWhiteSpace(input.NombreCompleto))
                    throw new ArgumentException("El nombre es obligatorio.", nameof(input.NombreCompleto));

                var nombreCompleto = input.NombreCompleto.Trim();
                var partes = nombreCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string nombres, apellidos;
                if (partes.Length == 1)
                {
                    nombres = partes[0];
                    apellidos = "";
                }
                else
                {
                    nombres = partes[0];
                    apellidos = string.Join(' ', partes.Skip(1));
                }
                var nombreVO = NombrePersona.Crear(nombres, apellidos);
                var telefonoVO = string.IsNullOrWhiteSpace(input.Celular)
                    ? null
                    : Telefono.FromTexto(input.Celular.Trim());

            // Identidad del usuario (global). En integración con Identidad, este ID vendrá del otro BC.
            var usuarioId = UsuarioId.From(Guid.NewGuid());

            // Crear aggregate en estado Invitado
            var nuevo = UsuarioEmpresa.Crear(
                empresaId,
                usuarioId,
                documento: null,                 // DNI opcional; si decides mapearlo, úsalo aquí
                nombre: nombreVO,
                emailContacto: emailVO,
                telefonoContacto: telefonoVO,
                rolesEmpresaIds: null,
                accesosIniciales: input.Accesos.Select(a =>
                    (EstablecimientoId.From(a.EstablecimientoId), a.RolIds.AsEnumerable()))
            );

            // Persistencia
            await _repo.AddAsync(nuevo, ct);
            await _uow.SaveChangesAsync(ct);

            // Salida
            var accesosOut = nuevo.Accesos
                .Select(a => new RegistrarUsuarioEmpresaOutputDto.AccesoResult(
                    a.EstablecimientoId.Value,
                    a.RolIds.ToList().AsReadOnly()))
                .ToList()
                .AsReadOnly();

            return new RegistrarUsuarioEmpresaOutputDto(
                empresaId.Value,
                usuarioId.Value,
                emailVO.Value,
                nuevo.Estado.ToString(),
                nuevo.Version,
                accesosOut
            );
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// Domain
using ConfiguracionSistemaBC.Domain.Aggregates;           // UsuarioEmpresa, UsuarioEmpresaEstado
using ConfiguracionSistemaBC.Domain.Repositories;         // IUsuarioEmpresaRepository

// Shared Kernel
using SharedKernel.Application.Interfaces;                // ITenantContext
using SharedKernel.ValueObjects;                          // EmpresaId, EstablecimientoId

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Consulta paginada de usuarios de la empresa actual (multiempresa),
    /// con filtros por estado, establecimiento y/o rol.
    /// </summary>
    public sealed class ConsultarUsuariosEmpresaUseCase
    {
        private readonly ITenantContext _tenant;
        private readonly IUsuarioEmpresaRepository _repo;

        public ConsultarUsuariosEmpresaUseCase(
            ITenantContext tenantContext,
            IUsuarioEmpresaRepository usuarioRepo)
        {
            _tenant = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
            _repo   = usuarioRepo    ?? throw new ArgumentNullException(nameof(usuarioRepo));
        }

        public async Task<ConsultarUsuariosEmpresaOutputDto> HandleAsync(
            ConsultarUsuariosEmpresaInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("EmpresaId no disponible en el contexto.");

            // Validación simple de paginación
            if (input.Page < 1) throw new ArgumentOutOfRangeException(nameof(input.Page), "Page debe ser >= 1.");
            if (input.PageSize < 1 || input.PageSize > 200) throw new ArgumentOutOfRangeException(nameof(input.PageSize), "PageSize debe estar entre 1 y 200.");

            // Mapear filtros
            UsuarioEmpresaEstado? estado = ParseEstadoOrNull(input.Estado);
            EstablecimientoId? estId = input.EstablecimientoId.HasValue ? EstablecimientoId.From(input.EstablecimientoId.Value) : null;
            Guid? rolId = input.RolId.HasValue && input.RolId.Value != Guid.Empty ? input.RolId : null;

            var skip = (input.Page - 1) * input.PageSize;
            var take = input.PageSize;

            // Consulta principal
            var lista = await _repo.ListAsync(
                empresaId,
                estado,
                estId,
                rolId,
                skip,
                take,
                ct);

            // Total: solo disponible por estado (el repo actual no cuenta por establecimiento/rol)
            int? total = null;
            if (estId is null && rolId is null)
            {
                var cnt = await _repo.CountAsync(empresaId, estado, ct);
                total = cnt;
            }

            // Mapear salida
            var items = lista.Select(x => new ConsultarUsuariosEmpresaOutputDto.UsuarioEmpresaItem
            {
                UsuarioId = x.UsuarioId.Value,
                Estado = x.Estado switch
                {
                    UsuarioEmpresaEstado.Invitado => "INVITADO",
                    UsuarioEmpresaEstado.Habilitado => "HABILITADO",
                    UsuarioEmpresaEstado.Inhabilitado => "INHABILITADO",
                    _ => x.Estado.ToString().ToUpperInvariant()
                },
                NombreCompleto = x.Nombre.Completo,
                Email = x.EmailContacto.Value,
                Telefono = x.TelefonoContacto?.UnirParaMostrar() ?? string.Empty,
                RolesEmpresaIds = x.RolesEmpresaIds.ToList(),
                Accesos = x.Accesos.Select(a => new ConsultarUsuariosEmpresaOutputDto.AccesoItem
                {
                    EstablecimientoId = a.EstablecimientoId.Value,
                    RolIds = a.RolIds.ToList()
                }).ToList()
            }).ToList();

            return new ConsultarUsuariosEmpresaOutputDto
            {
                Page = input.Page,
                PageSize = input.PageSize,
                ItemsCount = items.Count,
                Total = total,
                Items = items
            };
        }

        private static UsuarioEmpresaEstado? ParseEstadoOrNull(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var v = raw.Trim().ToUpperInvariant();

            // Permite nombres y números ("0","1","2")
            return v switch
            {
                "INVITADO"     => UsuarioEmpresaEstado.Invitado,
                "HABILITADO"   => UsuarioEmpresaEstado.Habilitado,
                "INHABILITADO" => UsuarioEmpresaEstado.Inhabilitado,
                "0"            => UsuarioEmpresaEstado.Invitado,
                "1"            => UsuarioEmpresaEstado.Habilitado,
                "2"            => UsuarioEmpresaEstado.Inhabilitado,
                _ => null
            };
        }
    }
}

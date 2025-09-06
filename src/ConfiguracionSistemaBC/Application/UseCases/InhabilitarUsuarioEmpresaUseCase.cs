using System;
using System.Threading;
using System.Threading.Tasks;

// Dominio / Repos
using ConfiguracionSistemaBC.Domain.Aggregates;           // UsuarioEmpresa, UsuarioEmpresaEstado
using ConfiguracionSistemaBC.Domain.Repositories;         // IUsuarioEmpresaRepository, IUnitOfWork

// Shared Kernel
using SharedKernel.Application.Interfaces;                // ITenantContext
using SharedKernel.ValueObjects;                          // EmpresaId, UsuarioId

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Inhabilita la membresía de un usuario en una empresa.
    /// - No elimina (si tuvo acciones previas, nunca se elimina).
    /// - Idempotente: si ya estaba inhabilitado, no fuerza update ni SaveChanges.
    /// </summary>
    public sealed class InhabilitarUsuarioEmpresaUseCase
    {
        private readonly ITenantContext? _tenant;
        private readonly IUsuarioEmpresaRepository _repo;
        private readonly IUnitOfWork _uow;

        public InhabilitarUsuarioEmpresaUseCase(
            IUsuarioEmpresaRepository repo,
            IUnitOfWork uow,
            ITenantContext? tenantContext = null)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow  = uow  ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenantContext;
        }

        public async Task<InhabilitarUsuarioEmpresaOutputDto> HandleAsync(
            InhabilitarUsuarioEmpresaInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (input.UsuarioId == Guid.Empty) throw new ArgumentNullException(nameof(input.UsuarioId));
            if (string.IsNullOrWhiteSpace(input.Razon)) throw new ArgumentNullException(nameof(input.Razon));

            // Empresa desde tenant o DTO
            var empresaId = _tenant?.EmpresaId ?? MapEmpresaIdFromDto(input.EmpresaId);
            var usuarioId = UsuarioId.From(input.UsuarioId);

            // Cargar agregado
            var agg = await _repo.GetAsync(empresaId, usuarioId, ct);
            if (agg is null)
                throw new KeyNotFoundException("Usuario de empresa no encontrado.");

            var yaEstabaInhabilitado = agg.Estado == UsuarioEmpresaEstado.Inhabilitado;

            if (!yaEstabaInhabilitado)
            {
                // Ejecutar acción de dominio
                agg.Inhabilitar(input.Razon);

                // Persistir con concurrencia optimista
                await _repo.UpdateAsync(agg, input.ExpectedVersion, ct);
                await _uow.SaveChangesAsync(ct);
            }

            return new InhabilitarUsuarioEmpresaOutputDto
            {
                EmpresaId = empresaId.Value,
                UsuarioId = usuarioId.Value,
                Estado = agg.Estado.ToString(),
                NuevaVersion = agg.Version,         // si ya estaba inhabilitado, se mantiene
                YaEstabaInhabilitado = yaEstabaInhabilitado
            };
        }

        private static EmpresaId MapEmpresaIdFromDto(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                throw new ArgumentNullException(nameof(raw), "EmpresaId es obligatorio si no hay contexto de tenant.");
            return EmpresaId.From(raw.Trim());
        }
    }
}

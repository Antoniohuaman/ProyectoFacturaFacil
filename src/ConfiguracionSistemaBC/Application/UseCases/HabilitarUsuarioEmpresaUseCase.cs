using System;
using System.Threading;
using System.Threading.Tasks;

// Domain
using ConfiguracionSistemaBC.Domain.Aggregates;           // UsuarioEmpresa
using ConfiguracionSistemaBC.Domain.Repositories;         // IUsuarioEmpresaRepository
using ConfiguracionSistemaBC.Application.Interfaces;      // IUnitOfWork

// Shared Kernel
using SharedKernel.Application.Interfaces;                // ITenantContext
using SharedKernel.ValueObjects;                          // EmpresaId, UsuarioId

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Habilita a un usuario dentro de la empresa (multiempresa).
    /// - Carga el agregado por (EmpresaId actual, UsuarioId).
    /// - Si no existe, lanza KeyNotFoundException.
    /// - Invoca MarcarConfirmadoPorIdentidad() para asegurar estado Habilitado (idempotente).
    /// - Persiste con concurrencia optimista (ExpectedVersion).
    /// </summary>
    public sealed class HabilitarUsuarioEmpresaUseCase
    {
        private readonly ITenantContext _tenant;
        private readonly IUsuarioEmpresaRepository _usuarioRepo;
        private readonly IUnitOfWork _uow;

        public HabilitarUsuarioEmpresaUseCase(
            IUsuarioEmpresaRepository usuarioRepo,
            IUnitOfWork uow,
            ITenantContext tenantContext)
        {
            _usuarioRepo = usuarioRepo ?? throw new ArgumentNullException(nameof(usuarioRepo));
            _uow         = uow ?? throw new ArgumentNullException(nameof(uow));
            _tenant      = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        }

        public async Task<HabilitarUsuarioEmpresaOutputDto> HandleAsync(
            HabilitarUsuarioEmpresaInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (input.UsuarioId == Guid.Empty)
                throw new ArgumentNullException(nameof(input.UsuarioId), "UsuarioId es obligatorio.");

            // Empresa (tenant) actual
            var empresaId = _tenant.EmpresaId;

            // Cargar agregado
            var usuarioId = UsuarioId.From(input.UsuarioId);
            var agregado = await _usuarioRepo.GetAsync(empresaId, usuarioId, ct);
            if (agregado is null)
                throw new KeyNotFoundException("Usuario no encontrado en la empresa.");

            // Asegurar estado Habilitado (idempotente)
            agregado.MarcarConfirmadoPorIdentidad(); // si ya está habilitado, no cambia

            // Persistir (concurrencia optimista)
            await _usuarioRepo.UpdateAsync(agregado, input.ExpectedVersion, ct);
            await _uow.CommitAsync(ct);

            // Salida
            return new HabilitarUsuarioEmpresaOutputDto
            {
                UsuarioId = input.UsuarioId,
                Estado = agregado.Estado.ToString(),
                Version = agregado.Version
            };
        }
    }
}

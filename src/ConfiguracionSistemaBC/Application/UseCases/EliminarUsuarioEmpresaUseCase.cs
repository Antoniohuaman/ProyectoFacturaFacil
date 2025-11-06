using System;
using System.Threading;
using System.Threading.Tasks;

// Domain
using ConfiguracionSistemaBC.Domain.Repositories; // IUsuarioEmpresaRepository
using ConfiguracionSistemaBC.Application.Interfaces; // IUnitOfWork
using ConfiguracionSistemaBC.Domain.Aggregates;   // UsuarioEmpresa

// Shared Kernel
using SharedKernel.Application.Interfaces;        // ITenantContext
using SharedKernel.ValueObjects;                  // EmpresaId, UsuarioId

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Elimina físicamente a un usuario de la empresa (tenant) actual.
    /// Regla: solo si <see cref="UsuarioEmpresa.PuedeSerEliminado"/> es true.
    /// Si no puede, se debe inhabilitar en lugar de eliminar.
    /// </summary>
    public sealed class EliminarUsuarioEmpresaUseCase
    {
        private readonly ITenantContext _tenant;
        private readonly IUsuarioEmpresaRepository _usuarioRepo;
        private readonly IUnitOfWork _uow;

        public EliminarUsuarioEmpresaUseCase(
            IUsuarioEmpresaRepository usuarioRepo,
            IUnitOfWork uow,
            ITenantContext tenantContext)
        {
            _usuarioRepo = usuarioRepo ?? throw new ArgumentNullException(nameof(usuarioRepo));
            _uow         = uow ?? throw new ArgumentNullException(nameof(uow));
            _tenant      = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        }

        public async Task<EliminarUsuarioEmpresaOutputDto> HandleAsync(
            EliminarUsuarioEmpresaInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (input.UsuarioId == Guid.Empty)
                throw new ArgumentNullException(nameof(input.UsuarioId), "UsuarioId es obligatorio.");
            if (input.ExpectedVersion < 0)
                throw new ArgumentOutOfRangeException(nameof(input.ExpectedVersion), "ExpectedVersion inválida.");

            var empresaId = _tenant.EmpresaId 
                ?? throw new InvalidOperationException("EmpresaId no disponible en el contexto.");

            var usuarioId = UsuarioId.From(input.UsuarioId);

            // 1) Cargar agregado
            var agg = await _usuarioRepo.GetAsync(empresaId, usuarioId, ct);
            if (agg is null)
                throw new KeyNotFoundException("Usuario no encontrado en la empresa.");

            // 2) Regla: solo eliminar si no tiene acciones relevantes
            if (!agg.PuedeSerEliminado)
                throw new InvalidOperationException(
                    "No se puede eliminar el usuario porque ya registra acciones relevantes. " +
                    "Debes inhabilitarlo en lugar de eliminarlo.");

            // 3) Eliminar con concurrencia optimista
            await _usuarioRepo.DeleteAsync(empresaId, usuarioId, input.ExpectedVersion, ct);

            // 4) Persistir
            await _uow.CommitAsync(ct);

            // 5) Salida
            return new EliminarUsuarioEmpresaOutputDto
            {
                EmpresaId  = empresaId.Value,
                UsuarioId  = input.UsuarioId,
                Eliminado  = true
            };
        }
    }
}

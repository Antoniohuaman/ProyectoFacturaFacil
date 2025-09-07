using System;
using System.Threading;
using System.Threading.Tasks;
using CatalogoArticulosBC.Domain.Repositories;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Application.UseCases.EliminarTodosLosProductos
{
    public interface IEliminarTodosLosProductosUseCase
    {
        Task<EliminarTodosLosProductosOutputDto> ExecuteAsync(
            EliminarTodosLosProductosInputDto input,
            CancellationToken ct = default);
    }

    /// <summary>
    /// Vaciado total y definitivo de los productos de la empresa actual (tenant/empresa del contexto).
    /// </summary>
    public sealed class EliminarTodosLosProductosUseCase : IEliminarTodosLosProductosUseCase
    {
        private readonly IProductoRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;

        public EliminarTodosLosProductosUseCase(
            IProductoRepository repo,
            IUnitOfWork uow,
            ITenantContext tenant)
        {
            _repo   = repo   ?? throw new ArgumentNullException(nameof(repo));
            _uow    = uow    ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<EliminarTodosLosProductosOutputDto> ExecuteAsync(
            EliminarTodosLosProductosInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (!input.Confirmar)
                throw new BusinessRuleException("Debe confirmar explícitamente la eliminación total de productos.");

            EmpresaId empresaId = _tenant.EmpresaId;

            // Eliminación masiva por Empresa
            int eliminados = await _repo.DeleteAllAsync(empresaId, ct);

            await _uow.CommitAsync();

            return new EliminarTodosLosProductosOutputDto
            {
                EmpresaId = empresaId.Value,
                CantidadEliminada = eliminados,
                EjecutadoEnUtc = DateTimeOffset.UtcNow,
                Exitoso = true
            };
        }
    }
}

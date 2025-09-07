using System;
using System.Threading;
using System.Threading.Tasks;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.Repositories;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;

namespace CatalogoArticulosBC.Application.UseCases.EliminarProductoSimple
{
    public interface IEliminarProductoSimpleUseCase
    {
        Task<EliminarProductoSimpleOutputDto> ExecuteAsync(EliminarProductoSimpleInputDto input, CancellationToken ct = default);
    }

    /// <summary>
    /// Elimina libremente un producto (sin restricciones adicionales).
    /// </summary>
    public sealed class EliminarProductoSimpleUseCase : IEliminarProductoSimpleUseCase
    {
        private readonly IProductoRepository _repo;
        private readonly IUnitOfWork _uow;

        public EliminarProductoSimpleUseCase(IProductoRepository repo, IUnitOfWork uow)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        }

        public async Task<EliminarProductoSimpleOutputDto> ExecuteAsync(EliminarProductoSimpleInputDto input, CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (input.ProductoId == Guid.Empty) throw new ArgumentException("ProductoId inválido.", nameof(input.ProductoId));

            // 1) Buscar agregado
            var producto = await _repo.GetByIdAsync(input.ProductoId);
            if (producto is null)
                throw new NotFoundException(nameof(ProductoSimple), input.ProductoId.ToString());

            // 2) Capturamos datos para el output antes de eliminar
            var sku = producto.Sku.Valor;
            var nombre = producto.Nombre.Valor;

            // 3) Eliminar y confirmar
            await _repo.DeleteAsync(producto);
            await _uow.CommitAsync();

            // 4) Respuesta
            return new EliminarProductoSimpleOutputDto
            {
                ProductoId = input.ProductoId,
                Sku = sku,
                Nombre = nombre,
                Eliminado = true
            };
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using CatalogoArticulosBC.Domain.Repositories;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;

namespace CatalogoArticulosBC.Application.UseCases.InhabilitarProducto
{
    public interface IInhabilitarProductoUseCase
    {
        Task<InhabilitarProductoOutputDto> ExecuteAsync(
            InhabilitarProductoInputDto input,
            CancellationToken ct = default);
    }

    /// <summary>
    /// Inhabilita un producto (cambio de estado lógico). No elimina.
    /// </summary>
    public sealed class InhabilitarProductoUseCase : IInhabilitarProductoUseCase
    {
        private readonly IProductoRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;

        public InhabilitarProductoUseCase(
            IProductoRepository repo,
            IUnitOfWork uow,
            ITenantContext tenant)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow  = uow  ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<InhabilitarProductoOutputDto> ExecuteAsync(
            InhabilitarProductoInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (input.ProductoId == Guid.Empty)
                throw new BusinessRuleException("El ProductoId es obligatorio.");
            if (string.IsNullOrWhiteSpace(input.Motivo))
                throw new BusinessRuleException("El motivo de inhabilitación es obligatorio.");

            var producto = await _repo.GetByIdAsync(input.ProductoId);
            if (producto is null)
                throw new NotFoundException($"No se encontró el producto con Id '{input.ProductoId}'.");

            // Si ya está inhabilitado, devolvemos salida idempotente sin tocar repositorio.
            if (!producto.Habilitado)
            {
                return new InhabilitarProductoOutputDto
                {
                    EmpresaId = _tenant.EmpresaId.Value,
                    ProductoId = producto.ProductoId,
                    Sku = producto.Sku?.Valor ?? string.Empty,
                    Nombre = producto.Nombre?.Valor ?? string.Empty,
                    Motivo = input.Motivo.Trim(),
                    Habilitado = producto.Habilitado, // false
                    YaEstabaInhabilitado = true,
                    EjecutadoEnUtc = DateTimeOffset.UtcNow,
                    Exitoso = true
                };
            }

            // Inhabilitar
            producto.Deshabilitar(input.Motivo.Trim());

            // Persistir
            await _repo.UpdateAsync(producto);
            await _uow.CommitAsync();

            // Salida
            return new InhabilitarProductoOutputDto
            {
                EmpresaId = _tenant.EmpresaId.Value,
                ProductoId = producto.ProductoId,
                Sku = producto.Sku?.Valor ?? string.Empty,
                Nombre = producto.Nombre?.Valor ?? string.Empty,
                Motivo = input.Motivo.Trim(),
                Habilitado = producto.Habilitado, // false
                YaEstabaInhabilitado = false,
                EjecutadoEnUtc = DateTimeOffset.UtcNow,
                Exitoso = true
            };
        }
    }
}

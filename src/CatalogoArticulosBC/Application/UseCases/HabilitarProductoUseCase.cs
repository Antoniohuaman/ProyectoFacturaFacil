using System;
using System.Threading;
using System.Threading.Tasks;
using CatalogoArticulosBC.Domain.Repositories;
using CatalogoArticulosBC.Application.Interfaces; // IUnitOfWork from Application layer
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;

namespace CatalogoArticulosBC.Application.UseCases.HabilitarProducto
{
    public interface IHabilitarProductoUseCase
    {
        Task<HabilitarProductoOutputDto> ExecuteAsync(
            HabilitarProductoInputDto input,
            CancellationToken ct = default);
    }

    /// <summary>
    /// Habilita un producto (cambio de estado lógico). Es idempotente si ya estaba habilitado.
    /// </summary>
    public sealed class HabilitarProductoUseCase : IHabilitarProductoUseCase
    {
    private readonly IProductoRepository _repo;
    private readonly CatalogoArticulosBC.Application.Interfaces.IUnitOfWork _uow;
    private readonly ITenantContext _tenant;

        public HabilitarProductoUseCase(
            IProductoRepository repo,
            CatalogoArticulosBC.Application.Interfaces.IUnitOfWork uow,
            ITenantContext tenant)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow  = uow  ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<HabilitarProductoOutputDto> ExecuteAsync(
            HabilitarProductoInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (input.ProductoId == Guid.Empty)
                throw new BusinessRuleException("El ProductoId es obligatorio.");
            if (string.IsNullOrWhiteSpace(input.Usuario))
                throw new BusinessRuleException("El usuario es obligatorio para habilitar el producto.");

            var producto = await _repo.GetByIdAsync(input.ProductoId, _tenant.EmpresaId);
            if (producto is null)
                throw new NotFoundException($"No se encontró el producto con Id '{input.ProductoId}'.");

            // Idempotencia: si ya está habilitado no persistimos cambios
            if (producto.Habilitado)
            {
                return new HabilitarProductoOutputDto
                {
                    EmpresaId = _tenant.EmpresaId.Value,
                    ProductoId = producto.ProductoId,
                    Sku = producto.Sku?.Valor ?? string.Empty,
                    Nombre = producto.Nombre?.Valor ?? string.Empty,
                    Usuario = input.Usuario.Trim(),
                    Motivo = input.Motivo?.Trim(),
                    Habilitado = producto.Habilitado, // true
                    YaEstabaHabilitado = true,
                    EjecutadoEnUtc = DateTimeOffset.UtcNow,
                    Exitoso = true
                };
            }

            // Habilitar
            producto.Habilitar(input.Usuario.Trim(), input.Motivo?.Trim());

            // Persistir
            await _repo.UpdateAsync(producto);
            await _uow.CommitAsync();

            // Salida
            return new HabilitarProductoOutputDto
            {
                EmpresaId = _tenant.EmpresaId.Value,
                ProductoId = producto.ProductoId,
                Sku = producto.Sku?.Valor ?? string.Empty,
                Nombre = producto.Nombre?.Valor ?? string.Empty,
                Usuario = input.Usuario.Trim(),
                Motivo = input.Motivo?.Trim(),
                Habilitado = producto.Habilitado, // true
                YaEstabaHabilitado = false,
                EjecutadoEnUtc = DateTimeOffset.UtcNow,
                Exitoso = true
            };
        }
    }
}

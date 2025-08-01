using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.Interfaces;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.ValueObjects;
using CatalogoArticulosBC.Domain.Entities;

namespace CatalogoArticulosBC.Adapters.Output.Persistence.InMemory
{
    public class InMemoryCatalogoArticulosRepository : ICatalogoArticulosRepository
    {
        private readonly ConcurrentDictionary<SKU, ProductoSimple> _productos = new();

        public Task AddAsync(ProductoSimple producto, CancellationToken ct = default)
            => AddProductoSimpleAsync(producto);

        public Task<ProductoSimple?> GetBySkuAsync(string sku, CancellationToken ct = default)
        {
            var key = new SKU(sku);
            return GetProductoSimpleBySkuAsync(key);
        }

        public Task<ProductoSimple?> GetProductoSimpleBySkuAsync(SKU sku)
        {
            if (_productos.TryGetValue(sku, out var ps) && ps.Sku == sku)
                return Task.FromResult<ProductoSimple?>(ps);
            return Task.FromResult<ProductoSimple?>(null);
        }

        public Task AddProductoSimpleAsync(ProductoSimple producto)
        {
            if (!_productos.TryAdd(producto.Sku, producto))
                throw new InvalidOperationException($"El SKU {producto.Sku} ya existe en el catálogo");
            return Task.CompletedTask;
        }

        public Task UpdateAsync(object producto)
        {
            if (producto is ProductoSimple simple)
            {
                _productos[simple.Sku] = simple;
            }
            else
            {
                throw new ArgumentException("Tipo de producto no soportado");
            }
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<ProductoSimple>> ListarProductosSimplesAsync()
        {
            var lista = _productos.Values
                                  .ToList();
            return Task.FromResult((IReadOnlyCollection<ProductoSimple>)lista);
        }

        public Task<IReadOnlyCollection<object>> ListarAsync() => throw new NotImplementedException();

        public Task<ProductoSimple?> GetByIdAsync(Guid productoId)
        {
            var producto = _productos.Values
            .FirstOrDefault(p => p.ProductoId == productoId);
            return Task.FromResult(producto);
        }

        public Task<IReadOnlyCollection<ProductoSimple>> ListarAsync(int pagina, int tamano, string filtrosJson) => throw new NotImplementedException();

        public Task EliminarProductoSimpleAsync(Guid productoId)
        {
            var producto = _productos.Values
                .FirstOrDefault(p => p.ProductoId == productoId);
            if (producto != null)
            {
                _productos.TryRemove(producto.Sku, out _);
            }
            return Task.CompletedTask;
        }

        public Task<ProductoSimple?> GetProductoSimpleByIdAsync(Guid productoId)
        {
            var producto = _productos.Values
                .FirstOrDefault(p => p.ProductoId == productoId);
            return Task.FromResult(producto);
        }
    }
}
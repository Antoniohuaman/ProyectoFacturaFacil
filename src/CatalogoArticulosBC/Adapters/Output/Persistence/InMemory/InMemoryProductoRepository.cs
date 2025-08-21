using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.ValueObjects;
using CatalogoArticulosBC.Domain.Repositories;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Adapters.Output.Persistence.InMemory
{
    public class InMemoryCatalogoArticulosRepository : IProductoRepository
    {
    private readonly ConcurrentDictionary<Sku, ProductoSimple> _productos = new();

        public Task<ProductoSimple?> GetByIdAsync(Guid id)
        {
            var producto = _productos.Values.FirstOrDefault(p => p.ProductoId == id);
            return Task.FromResult(producto);
        }

        public Task<ProductoSimple?> GetBySkuAsync(Sku sku)
        {
            _productos.TryGetValue(sku, out var producto);
            return Task.FromResult(producto);
        }

        public Task<IReadOnlyCollection<ProductoSimple>> GetAllAsync()
        {
            return Task.FromResult((IReadOnlyCollection<ProductoSimple>)_productos.Values.ToList());
        }

        public Task AddAsync(ProductoSimple producto)
        {
            _productos.TryAdd(producto.Sku, producto);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ProductoSimple producto)
        {
            _productos[producto.Sku] = producto;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(ProductoSimple producto)
        {
            _productos.TryRemove(producto.Sku, out _);
            return Task.CompletedTask;
        }
    }
}
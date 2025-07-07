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
        private readonly ConcurrentDictionary<SKU, object> _productos = new();

        public Task AddAsync(ProductoSimple producto, CancellationToken ct = default)
            => AddProductoSimpleAsync(producto);

        public Task<ProductoSimple?> GetBySkuAsync(string sku, CancellationToken ct = default)
        {
            var key = new SKU(sku);
            return GetProductoSimpleBySkuAsync(key);
        }

        public Task<ProductoSimple?> GetProductoSimpleBySkuAsync(SKU sku)
        {
             if (_productos.TryGetValue(sku, out var p) && p is ProductoSimple ps && ps.Sku == sku)
                 return Task.FromResult<ProductoSimple?>(ps);
    // Elimina la siguiente línea porque ProductoVariante no es ProductoSimple
    // if (_productos.TryGetValue(sku, out var v) && v is ProductoVariante pv && pv.Sku == sku)
    //     return Task.FromResult<ProductoSimple?>(pv as ProductoSimple);
            return Task.FromResult<ProductoSimple?>(null);
        }

        public Task AddProductoSimpleAsync(ProductoSimple producto)
        {
            if (!_productos.TryAdd(producto.Sku, producto))
                throw new InvalidOperationException($"El SKU {producto.Sku} ya existe en el catálogo");
            return Task.CompletedTask;
        }

        public Task<ProductoVariante?> GetProductoVarianteBySkuAsync(SKU sku)
{
    if (_productos.TryGetValue(sku, out var v) && v is ProductoVariante pv && pv.Sku == sku)
        return Task.FromResult<ProductoVariante?>(pv);
    return Task.FromResult<ProductoVariante?>(null);
}
        public Task AddProductoVarianteAsync(ProductoVariante producto)
        {
        if (!_productos.TryAdd(producto.Sku, producto))
        throw new InvalidOperationException($"El SKU {producto.Sku} ya existe en el catálogo");
        return Task.CompletedTask;
        }
        public Task<ProductoCombo?> GetProductoComboBySkuAsync(SKU sku) => throw new NotImplementedException();
        public Task AddProductoComboAsync(ProductoCombo producto) => throw new NotImplementedException();
        public Task UpdateAsync(object producto) => throw new NotImplementedException();

        public Task<IReadOnlyCollection<ProductoSimple>> ListarProductosSimplesAsync()
        {
            var lista = _productos.Values
                                  .OfType<ProductoSimple>()
                                  .ToList();
            return Task.FromResult((IReadOnlyCollection<ProductoSimple>)lista);
        }

        public Task<IReadOnlyCollection<object>> ListarAsync() => throw new NotImplementedException();
        public Task<ProductoSimple?> GetByIdAsync(Guid productoId) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<ProductoSimple>> ListarAsync(int pagina, int tamano, string filtrosJson) => throw new NotImplementedException();
    }
}
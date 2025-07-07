using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.Interfaces;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.ValueObjects;

namespace CatalogoArticulosBC.Adapters.Output.Persistence.InMemory
{
    public class InMemoryCatalogoArticulosRepository : ICatalogoArticulosRepository
    {
        // Diccionario clave = SKU, valor = objeto de dominio (ProductoSimple, Variante o Combo)
        private readonly ConcurrentDictionary<SKU, object> _productos = new();

        public Task<ProductoSimple?> GetProductoSimpleBySkuAsync(SKU sku)
        {
            if (_productos.TryGetValue(sku, out var p) && p is ProductoSimple ps && ps.Sku == sku)
                return Task.FromResult(ps);
            return Task.FromResult<ProductoSimple?>(null);
        }

        public Task AddProductoSimpleAsync(ProductoSimple producto)
        {
            if (!_productos.TryAdd(producto.Sku, producto))
                throw new InvalidOperationException($"El SKU {producto.Sku} ya existe en el catálogo");
            return Task.CompletedTask;
        }

        public Task UpdateProductoSimpleAsync(ProductoSimple producto)
        {
            _productos[producto.Sku] = producto;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<ProductoSimple>> ListarProductosSimplesAsync()
        {
            var lista = _productos.Values
                                  .OfType<ProductoSimple>()
                                  .ToList();
            return Task.FromResult((IReadOnlyCollection<ProductoSimple>)lista);
        }

        // Nota: Si tienes métodos específicos para variantes y combos, repite el patrón:
        // GetProductoVarianteBySkuAsync, AddProductoVarianteAsync, etc.
    }
}

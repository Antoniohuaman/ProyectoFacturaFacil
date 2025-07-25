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

        // IMPLEMENTACIÓN CORRECTA PARA COMBOS
        public Task<ProductoCombo?> GetProductoComboBySkuAsync(SKU sku)
        {
            if (_productos.TryGetValue(sku, out var c) && c is ProductoCombo combo && combo.Sku == sku)
                return Task.FromResult<ProductoCombo?>(combo);
            return Task.FromResult<ProductoCombo?>(null);
        }

        public Task AddProductoComboAsync(ProductoCombo producto)
        {
            if (!_productos.TryAdd(producto.Sku, producto))
                throw new InvalidOperationException($"El SKU {producto.Sku} ya existe en el catálogo");
            return Task.CompletedTask;
        }

        public Task UpdateAsync(object producto)
        {
            switch (producto)
            {
                case ProductoSimple simple:
                    _productos[simple.Sku] = simple;
                    break;
                case ProductoVariante variante:
                    _productos[variante.Sku] = variante;
                    break;
                case ProductoCombo combo:
                    _productos[combo.Sku] = combo;
                    break;
                default:
                    throw new ArgumentException("Tipo de producto no soportado");
            }
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<ProductoSimple>> ListarProductosSimplesAsync()
        {
            var lista = _productos.Values
                                  .OfType<ProductoSimple>()
                                  .ToList();
            return Task.FromResult((IReadOnlyCollection<ProductoSimple>)lista);
        }

        public Task<IReadOnlyCollection<object>> ListarAsync() => throw new NotImplementedException();

        public Task<ProductoSimple?> GetByIdAsync(Guid productoId)
        {
            var producto = _productos.Values
            .OfType<ProductoSimple>()
            .FirstOrDefault(p => p.ProductoId == productoId);
            return Task.FromResult(producto);
        }

        public Task<IReadOnlyCollection<ProductoSimple>> ListarAsync(int pagina, int tamano, string filtrosJson) => throw new NotImplementedException();

        public Task EliminarProductoSimpleAsync(Guid productoId)
        {
            var producto = _productos.Values
                .OfType<ProductoSimple>()
                .FirstOrDefault(p => p.ProductoId == productoId);
            if (producto != null)
            {
                _productos.TryRemove(producto.Sku, out _);
            }
            return Task.CompletedTask;
        }

        public Task<ProductoVariante?> GetProductoVarianteByIdAsync(Guid productoVarianteId)
        {
            var variante = _productos.Values
                .OfType<ProductoVariante>()
                .FirstOrDefault(v => v.ProductoVarianteId == productoVarianteId);
            return Task.FromResult(variante);
        }

        // --- MÉTODOS FALTANTES PARA SOPORTE DE VARIANTES POR PADRE E ID ---

        public Task<ProductoSimple?> GetProductoSimpleByIdAsync(Guid productoId)
        {
            var producto = _productos.Values
                .OfType<ProductoSimple>()
                .FirstOrDefault(p => p.ProductoId == productoId);
            return Task.FromResult(producto);
        }

        public Task<IEnumerable<ProductoVariante>> GetVariantesByPadreIdAsync(Guid productoPadreId)
        {
            var variantes = _productos.Values
                .OfType<ProductoVariante>()
                .Where(v => v.ProductoPadreId == productoPadreId)
                .ToList();
            return Task.FromResult<IEnumerable<ProductoVariante>>(variantes);
        }
    }
}
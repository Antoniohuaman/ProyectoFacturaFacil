using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.Entities;
using CatalogoArticulosBC.Domain.Filters;
using CatalogoArticulosBC.Domain.Repositories;
using CatalogoArticulosBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Adapters.Output.Persistence.InMemory
{
    /// <summary>
    /// Repositorio en memoria para pruebas. 
    /// Clave primaria en memoria: Sku; índice auxiliar por ProductoId para soportar updates con cambio de SKU.
    /// </summary>
    public class InMemoryCatalogoArticulosRepository : IProductoRepository
    {
        private readonly ConcurrentDictionary<Sku, ProductoSimple> _productos = new();
        private readonly ConcurrentDictionary<Guid, Sku> _indexById = new();
        private readonly object _gate = new(); // para mantener consistencia entre ambos diccionarios

        // ===== CRUD básico =====

        public Task<ProductoSimple?> GetByIdAsync(Guid id)
        {
            lock (_gate)
            {
                if (_indexById.TryGetValue(id, out var sku) && _productos.TryGetValue(sku, out var p))
                    return Task.FromResult<ProductoSimple?>(p);

                return Task.FromResult<ProductoSimple?>(null);
            }
        }

        public Task<ProductoSimple?> GetBySkuAsync(Sku sku)
        {
            _productos.TryGetValue(sku, out var producto);
            return Task.FromResult(producto);
        }

        public Task<IReadOnlyCollection<ProductoSimple>> GetAllAsync()
        {
            var list = _productos.Values.ToList();
            return Task.FromResult((IReadOnlyCollection<ProductoSimple>)list);
        }

        public Task AddAsync(ProductoSimple producto, CancellationToken ct = default)
        {
            lock (_gate)
            {
                _productos[producto.Sku] = producto;
                _indexById[producto.ProductoId] = producto.Sku;
            }
            return Task.CompletedTask;
        }

        // Compatibilidad con código antiguo
        public Task AddAsync(ProductoSimple producto)
            => AddAsync(producto, CancellationToken.None);

        /// <summary>
        /// Actualiza el producto. Si el SKU cambió, reindexa: borra la clave anterior y escribe la nueva.
        /// </summary>
        public Task UpdateAsync(ProductoSimple producto)
        {
            lock (_gate)
            {
                if (_indexById.TryGetValue(producto.ProductoId, out var oldSku) && !oldSku.Equals(producto.Sku))
                {
                    _productos.TryRemove(oldSku, out _);
                }

                _productos[producto.Sku] = producto;
                _indexById[producto.ProductoId] = producto.Sku;
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(ProductoSimple producto)
        {
            lock (_gate)
            {
                _productos.TryRemove(producto.Sku, out _);
                _indexById.TryRemove(producto.ProductoId, out _);
            }
            return Task.CompletedTask;
        }

        // ===== Soporte a políticas de unicidad / búsquedas =====

        public Task<bool> ExisteSkuAsync(Sku sku, EmpresaId empresaId, CancellationToken ct = default)
        {
            // En memoria ignoramos empresaId (tenant) y validamos por SKU solamente.
            var exists = _productos.Values.Any(p => p.Sku.Equals(sku));
            return Task.FromResult(exists);
        }

        public Task<bool> ExistsByCodigoAsync(string codigo)
        {
            var exists = _productos.Values.Any(p => p.Sku.Valor == codigo);
            return Task.FromResult(exists);
        }

        public Task<bool> ExistsByNombreAsync(string nombre)
        {
            var exists = _productos.Values.Any(p => p.Nombre != null && p.Nombre.Valor == nombre);
            return Task.FromResult(exists);
        }

        public Task<ProductoSimple?> GetByCodigoBarrasAsync(string codigoBarras)
        {
            var producto = _productos.Values.FirstOrDefault(p => p.CodigoBarras?.Valor == codigoBarras);
            return Task.FromResult(producto);
        }

        public Task<ProductoSimple?> GetByCodigoFabricaAsync(string codigoFabrica)
        {
            var producto = _productos.Values.FirstOrDefault(p => p.CodigoFabrica?.Valor == codigoFabrica);
            return Task.FromResult(producto);
        }

        public Task<ProductoSimple?> GetByNombreAsync(string nombre)
        {
            var producto = _productos.Values.FirstOrDefault(p => p.Nombre != null && p.Nombre.Valor == nombre);
            return Task.FromResult(producto);
        }

        public Task<IEnumerable<ProductoSimple>> ListarPorCategoriaAsync(Categoria categoria)
        {
            var productos = _productos.Values.Where(p => p.Categoria != null && p.Categoria.Equals(categoria));
            return Task.FromResult(productos);
        }

        public Task<IEnumerable<ProductoSimple>> ListarHabilitadosAsync()
        {
            var productos = _productos.Values.Where(p => p.Habilitado);
            return Task.FromResult(productos);
        }

        public Task<IEnumerable<ProductoSimple>> ListarDeshabilitadosAsync()
        {
            var productos = _productos.Values.Where(p => !p.Habilitado);
            return Task.FromResult(productos);
        }

        public Task<IEnumerable<ProductoSimple>> BuscarPorFiltroAsync(FiltroProducto filtro)
        {
            var query = _productos.Values.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filtro.Nombre))
                query = query.Where(p => p.Nombre != null && p.Nombre.Valor.Contains(filtro.Nombre, StringComparison.OrdinalIgnoreCase));

            if (filtro.Categoria != null)
                query = query.Where(p => p.Categoria != null && p.Categoria.Equals(filtro.Categoria));

            if (filtro.Habilitado.HasValue)
                query = query.Where(p => p.Habilitado == filtro.Habilitado.Value);

            if (filtro.PrecioMin.HasValue)
                query = query.Where(p => p.PrecioVenta != null && p.PrecioVenta.Monto >= filtro.PrecioMin.Value);

            if (filtro.PrecioMax.HasValue)
                query = query.Where(p => p.PrecioVenta != null && p.PrecioVenta.Monto <= filtro.PrecioMax.Value);

            return Task.FromResult(query.AsEnumerable());
        }

        // ===== Importación / Exportación / Multimedia (stubs para tests) =====

        public Task ImportarProductosAsync(IEnumerable<ProductoSimple> productos)
        {
            lock (_gate)
            {
                foreach (var producto in productos)
                {
                    _productos[producto.Sku] = producto;
                    _indexById[producto.ProductoId] = producto.Sku;
                }
            }
            return Task.CompletedTask;
        }

        public Task<IEnumerable<ProductoSimple>> ExportarProductosAsync(FiltroExportacion filtro)
        {
            var query = _productos.Values.AsQueryable();

            if (filtro.Categoria != null)
                query = query.Where(p => p.Categoria != null && p.Categoria.Equals(filtro.Categoria));

            if (filtro.SoloHabilitados.HasValue && filtro.SoloHabilitados.Value)
                query = query.Where(p => p.Habilitado);

            return Task.FromResult(query.AsEnumerable());
        }

        public Task<IEnumerable<MultimediaProducto>> GetMultimediaByProductoIdAsync(Guid productoId)
        {
            // Stub: retorna lista vacía; implementar según modelo real si lo necesitas en tests.
            return Task.FromResult(Enumerable.Empty<MultimediaProducto>());
        }
    }
}

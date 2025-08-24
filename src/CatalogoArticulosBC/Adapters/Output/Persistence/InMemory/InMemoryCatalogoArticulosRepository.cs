using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.ValueObjects;
using CatalogoArticulosBC.Domain.Repositories;
using SharedKernel.ValueObjects;
using CatalogoArticulosBC.Domain.Entities;

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

        // Métodos nuevos agregados por la interfaz
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

        public Task<IEnumerable<ProductoSimple>> ListarPorCategoriaAsync(Guid categoriaId)
        {
            var categoria = new Categoria(categoriaId.ToString());
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
            if (!string.IsNullOrEmpty(filtro.Nombre))
                query = query.Where(p => p.Nombre != null && p.Nombre.Valor.Contains(filtro.Nombre));
            if (filtro.CategoriaId.HasValue)
            {
                var categoriaFiltro = new Categoria(filtro.CategoriaId.Value.ToString());
                query = query.Where(p => p.Categoria != null && p.Categoria.Equals(categoriaFiltro));
            }
            if (filtro.Habilitado.HasValue)
                query = query.Where(p => p.Habilitado == filtro.Habilitado.Value);
            if (filtro.PrecioMin.HasValue)
                query = query.Where(p => p.PrecioVenta != null && p.PrecioVenta.Monto >= filtro.PrecioMin.Value);
            if (filtro.PrecioMax.HasValue)
                query = query.Where(p => p.PrecioVenta != null && p.PrecioVenta.Monto <= filtro.PrecioMax.Value);
            return Task.FromResult(query.AsEnumerable());
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

        public Task ImportarProductosAsync(IEnumerable<ProductoSimple> productos)
        {
            foreach (var producto in productos)
            {
                _productos[producto.Sku] = producto;
            }
            return Task.CompletedTask;
        }

        public Task<IEnumerable<ProductoSimple>> ExportarProductosAsync(FiltroExportacion filtro)
        {
            var query = _productos.Values.AsQueryable();
            if (filtro.CategoriaId.HasValue)
            {
                var categoriaFiltro = new Categoria(filtro.CategoriaId.Value.ToString());
                query = query.Where(p => p.Categoria != null && p.Categoria.Equals(categoriaFiltro));
            }
            if (filtro.SoloHabilitados.HasValue && filtro.SoloHabilitados.Value)
                query = query.Where(p => p.Habilitado);
            return Task.FromResult(query.AsEnumerable());
        }

        public Task<IEnumerable<MultimediaProducto>> GetMultimediaByProductoIdAsync(Guid productoId)
        {
            // Stub: retorna lista vacía, implementar según modelo real
            return Task.FromResult(Enumerable.Empty<MultimediaProducto>());
        }
    }
}
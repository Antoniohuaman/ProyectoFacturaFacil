// ICatalogoArticulosRepository.cs
using CatalogoArticulosBC.Domain.Aggregates;
using System.Threading.Tasks;
using System.Collections.Generic;
using CatalogoArticulosBC.Domain.ValueObjects;

namespace CatalogoArticulosBC.Application.Interfaces
{
    public interface ICatalogoArticulosRepository
    {
        Task AddAsync(ProductoSimple producto, CancellationToken ct = default);
        Task<ProductoSimple?> GetBySkuAsync(string sku, CancellationToken ct = default);
        // …otros métodos CRUD…
        Task<ProductoSimple?> GetProductoSimpleBySkuAsync(SKU sku);
        Task AddProductoSimpleAsync(ProductoSimple producto);
        Task<ProductoVariante?> GetProductoVarianteBySkuAsync(SKU sku);
        Task AddProductoVarianteAsync(ProductoVariante producto);
        Task<ProductoCombo?> GetProductoComboBySkuAsync(SKU sku);
        Task AddProductoComboAsync(ProductoCombo producto);
        Task UpdateAsync(object producto); // O usa el tipo correcto
        Task<IReadOnlyCollection<ProductoSimple>> ListarProductosSimplesAsync();
        Task<IReadOnlyCollection<object>> ListarAsync(); // Ajusta el tipo según tu modelo
        Task<ProductoSimple?> GetByIdAsync(Guid productoId);
        Task<IReadOnlyCollection<ProductoSimple>> ListarAsync(int pagina, int tamano, string filtrosJson);
        Task EliminarProductoSimpleAsync(Guid productoId);
    }
}

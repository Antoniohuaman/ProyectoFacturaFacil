// ICatalogoArticulosRepository.cs
using CatalogoArticulosBC.Domain.Aggregates;

namespace CatalogoArticulosBC.Application.Interfaces
{
    public interface ICatalogoArticulosRepository
    {
        Task AddAsync(ProductoSimple producto, CancellationToken ct = default);
        Task<ProductoSimple?> GetBySkuAsync(string sku, CancellationToken ct = default);
        // …otros métodos CRUD…
    }
}

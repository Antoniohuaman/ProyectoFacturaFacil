// InMemoryCatalogoArticulosRepository.cs
using CatalogoArticulosBC.Application.Interfaces;
using CatalogoArticulosBC.Domain.Aggregates;
using System.Collections.Concurrent;

namespace CatalogoArticulosBC.Adapters.Output.Persistence.InMemory
{
    public class InMemoryCatalogoArticulosRepository : ICatalogoArticulosRepository
    {
        private static readonly ConcurrentDictionary<Guid, ProductoSimple> _store = new();

        public Task AddAsync(ProductoSimple producto, CancellationToken ct = default)
        {
            _store[producto.Id] = producto;
            return Task.CompletedTask;
        }

        public Task<ProductoSimple?> GetBySkuAsync(string sku, CancellationToken ct = default)
            => Task.FromResult(_store.Values.FirstOrDefault(p => p.Sku == sku));

        // …otros métodos…
    }
}



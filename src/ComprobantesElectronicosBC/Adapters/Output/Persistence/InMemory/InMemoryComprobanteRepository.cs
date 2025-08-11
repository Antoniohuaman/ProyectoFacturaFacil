using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using ComprobantesElectronicosBC.Domain.Aggregates;
using ComprobantesElectronicosBC.Domain.Repositories;

namespace ComprobantesElectronicosBC.Adapters.Output.Persistence.InMemory
{
    /// <summary>
    /// Repositorio en memoria para ComprobanteElectronico.
    /// Thread-safe y con índice por (Serie,Número).
    /// </summary>
    public sealed class InMemoryComprobanteRepository : IComprobanteRepository
    {
        private readonly ConcurrentDictionary<Guid, ComprobanteElectronico> _byId = new();
        private readonly ConcurrentDictionary<string, Guid> _bySerieNumero = new();

        private static string Key(string serie, int numero)
            => $"{(serie ?? string.Empty).Trim().ToUpperInvariant()}#{numero:D8}";

        public Task<ComprobanteElectronico?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            _byId.TryGetValue(id, out var agg);
            return Task.FromResult(agg);
        }

        public Task<ComprobanteElectronico?> GetBySerieNumeroAsync(string serie, int numero, CancellationToken ct = default)
        {
            var k = Key(serie, numero);
            if (_bySerieNumero.TryGetValue(k, out var id) && _byId.TryGetValue(id, out var agg))
                return Task.FromResult<ComprobanteElectronico?>(agg);

            return Task.FromResult<ComprobanteElectronico?>(null);
        }

        public Task<bool> ExistsSerieNumeroAsync(string serie, int numero, CancellationToken ct = default)
        {
            var exists = _bySerieNumero.ContainsKey(Key(serie, numero));
            return Task.FromResult(exists);
        }

        public Task AddAsync(ComprobanteElectronico aggregate, CancellationToken ct = default)
        {
            _byId[aggregate.ComprobanteId] = aggregate;
            IndexSerieNumeroIfPresent(aggregate);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ComprobanteElectronico aggregate, CancellationToken ct = default)
        {
            _byId[aggregate.ComprobanteId] = aggregate;
            IndexSerieNumeroIfPresent(aggregate);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Guid id, CancellationToken ct = default)
        {
            if (_byId.TryRemove(id, out var agg) && agg.SerieNumero is not null)
            {
                var k = Key(agg.SerieNumero.Serie, agg.SerieNumero.Numero);
                _bySerieNumero.TryRemove(k, out _);
            }
            return Task.CompletedTask;
        }

        private void IndexSerieNumeroIfPresent(ComprobanteElectronico agg)
        {
            if (agg.SerieNumero is null) return;
            var k = Key(agg.SerieNumero.Serie, agg.SerieNumero.Numero);
            _bySerieNumero[k] = agg.ComprobanteId;
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using GestionClientesBC.Application.Interfaces;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.ValueObjects;

namespace GestionClientesBC.Adapters.Output.Persistence.InMemory
{
    public class InMemoryGestionClientesRepository : IGestionClientesRepository
    {
        private readonly ConcurrentDictionary<DocumentoIdentidad, Cliente> _clientes = new();

        public Task<Cliente?> GetByDocumentoIdentidadAsync(DocumentoIdentidad docId)
        {
            _clientes.TryGetValue(docId, out var cliente);
            return Task.FromResult(cliente);
        }

        public Task AddClienteAsync(Cliente cliente)
        {
            if (!_clientes.TryAdd(cliente.DocumentoIdentidad, cliente))
                throw new InvalidOperationException("Cliente ya existe.");
            return Task.CompletedTask;
        }

        public Task UpdateClienteAsync(Cliente cliente)
        {
            _clientes[cliente.DocumentoIdentidad] = cliente;
            return Task.CompletedTask;
        }
    }
}
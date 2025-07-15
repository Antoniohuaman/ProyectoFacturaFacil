using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using GestionClientesBC.Application.Interfaces;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.ValueObjects;
using GestionClientesBC.Domain.Entities;

namespace GestionClientesBC.Adapters.Output.Persistence.InMemory
{
    public class InMemoryGestionClientesRepository : IGestionClientesRepository
    {
        private readonly ConcurrentDictionary<DocumentoIdentidad, Cliente> _clientes = new();

        public Task<Cliente?> GetByIdAsync(Guid clienteId)
        {
            var cliente = _clientes.Values.FirstOrDefault(c => c.ClienteId == clienteId);
            return Task.FromResult(cliente);
        }

        public Task<Cliente?> GetByDocumentoIdentidadAsync(DocumentoIdentidad docId)
        {
            _clientes.TryGetValue(docId, out var cliente);
            return Task.FromResult(cliente);
        }

        public Task AddAsync(Cliente cliente)
        {
            if (!_clientes.TryAdd(cliente.DocumentoIdentidad, cliente))
                throw new InvalidOperationException("Cliente ya existe.");
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Cliente cliente)
        {
            _clientes[cliente.DocumentoIdentidad] = cliente;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid clienteId)
        {
            var cliente = _clientes.Values.FirstOrDefault(c => c.ClienteId == clienteId);
            if (cliente != null)
            {
                _clientes.TryRemove(cliente.DocumentoIdentidad, out _);
            }
            return Task.CompletedTask;
        }
        public Task<ICollection<ContactoCliente>> ObtenerContactosPorClienteIdAsync(Guid clienteId)
{
    var cliente = _clientes.Values.FirstOrDefault(c => c.ClienteId == clienteId);
    ICollection<ContactoCliente> contactos = cliente?.Contactos.ToList() ?? new List<ContactoCliente>();
    return Task.FromResult(contactos);
}

public Task<ICollection<AdjuntoCliente>> ObtenerAdjuntosPorClienteIdAsync(Guid clienteId)
{
    var cliente = _clientes.Values.FirstOrDefault(c => c.ClienteId == clienteId);
    ICollection<AdjuntoCliente> adjuntos = cliente?.Adjuntos.ToList() ?? new List<AdjuntoCliente>();
    return Task.FromResult(adjuntos);
}

public Task<ICollection<OperacionCliente>> ObtenerOperacionesPorClienteIdAsync(Guid clienteId, DateTime desde)
{
    var cliente = _clientes.Values.FirstOrDefault(c => c.ClienteId == clienteId);
    ICollection<OperacionCliente> operaciones = cliente != null
        ? cliente.Operaciones.Where(o => o.FechaOperacion >= desde).ToList()
        : new List<OperacionCliente>();
    return Task.FromResult(operaciones);
}
    }
}
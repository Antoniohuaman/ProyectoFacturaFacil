
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Repositories;

namespace GestionClientesBC.Adapters.Output.Persistence.InMemory
{
	public class InMemoryGestionClientesRepository : IClienteRepository
	{
		private readonly Dictionary<Guid, Cliente> _clientes = new();

		public Task<Cliente?> GetByIdAsync(Guid clienteId)
			=> Task.FromResult(_clientes.TryGetValue(clienteId, out var cliente) ? cliente : null);

		public Task<IReadOnlyList<Cliente>> GetAllAsync(int? skip = null, int? take = null)
		{
			var query = _clientes.Values.AsQueryable();
			if (skip.HasValue) query = query.Skip(skip.Value);
			if (take.HasValue) query = query.Take(take.Value);
			return Task.FromResult((IReadOnlyList<Cliente>)query.ToList());
		}

		public Task AddAsync(Cliente cliente)
		{
			_clientes[cliente.ClienteId] = cliente;
			return Task.CompletedTask;
		}

		public Task UpdateAsync(Cliente cliente)
		{
			_clientes[cliente.ClienteId] = cliente;
			return Task.CompletedTask;
		}

		public Task DeleteAsync(Guid clienteId)
		{
			_clientes.Remove(clienteId);
			return Task.CompletedTask;
		}

		public Task DeleteManyAsync(IEnumerable<Guid> clienteIds)
		{
			foreach (var id in clienteIds)
				_clientes.Remove(id);
			return Task.CompletedTask;
		}

		public Task DeleteAllAsync()
		{
			_clientes.Clear();
			return Task.CompletedTask;
		}

		public Task<IReadOnlyList<Cliente>> SearchAsync(string? filtro, int? skip = null, int? take = null)
		{
			IEnumerable<Cliente> query = _clientes.Values;
			if (!string.IsNullOrWhiteSpace(filtro))
			{
				query = query.Where(c =>
					(c.Documento.Numero != null && c.Documento.Numero.Contains(filtro, StringComparison.OrdinalIgnoreCase)) ||
					(c.RazonSocial != null && c.RazonSocial.Valor != null && c.RazonSocial.Valor.Contains(filtro, StringComparison.OrdinalIgnoreCase)) ||
					(c.Nombres != null && c.Nombres.Valor != null && c.Nombres.Valor.Contains(filtro, StringComparison.OrdinalIgnoreCase))
				);
			}
			if (skip.HasValue) query = query.Skip(skip.Value);
			if (take.HasValue) query = query.Take(take.Value);
			return Task.FromResult((IReadOnlyList<Cliente>)query.ToList());
		}
	}
}

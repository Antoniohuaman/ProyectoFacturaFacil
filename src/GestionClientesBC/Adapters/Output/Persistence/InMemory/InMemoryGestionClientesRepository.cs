
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Repositories;
using SharedKernel.ValueObjects;
using SharedKernel.Exceptions;

namespace GestionClientesBC.Adapters.Output.Persistence.InMemory
{
	public class InMemoryGestionClientesRepository : IClienteRepository
	{
		// Indexación por EmpresaId + ClienteId
	private readonly Dictionary<(string EmpresaId, Guid ClienteId), Cliente> _clientes = new();
	private readonly Dictionary<(string EmpresaId, Guid ClienteId), int> _versions = new();


		   public Task<Cliente?> GetByIdAsync(EmpresaId empresaId, Guid clienteId)
		   {
			   if (empresaId is null) throw new ArgumentNullException(nameof(empresaId));
			   return Task.FromResult(_clientes.TryGetValue((empresaId.Valor, clienteId), out var cliente) ? cliente : null);
		   }


		   public Task<IReadOnlyList<Cliente>> GetAllAsync(EmpresaId empresaId, int? skip = null, int? take = null)
		   {
			   if (empresaId is null) throw new ArgumentNullException(nameof(empresaId));
			   var query = _clientes.Values.Where(c => c.EmpresaId == empresaId);
			   if (skip.HasValue) query = query.Skip(skip.Value);
			   if (take.HasValue) query = query.Take(take.Value);
			   return Task.FromResult((IReadOnlyList<Cliente>)query.ToList());
		   }


		   public Task AddAsync(Cliente cliente)
		   {
			   if (cliente is null) throw new ArgumentNullException(nameof(cliente));
			   _clientes[(cliente.EmpresaId.Valor, cliente.ClienteId)] = cliente;
			   _versions[(cliente.EmpresaId.Valor, cliente.ClienteId)] = cliente.Version;
			   return Task.CompletedTask;
		   }

		   public Task UpdateAsync(Cliente cliente, int expectedVersion)
		   {
			   if (cliente is null) throw new ArgumentNullException(nameof(cliente));
			   var key = (cliente.EmpresaId.Valor, cliente.ClienteId);
			   var current = _versions.TryGetValue(key, out var ver) ? ver : (int?)null;
			   if (current is null)
			   {
				   throw new NotFoundException("Cliente", cliente.ClienteId.ToString());
			   }
			   if (expectedVersion != current.Value)
			   {
				   throw new ConcurrencyException(
					   aggregate: nameof(Cliente),
					   aggregateId: cliente.ClienteId.ToString(),
					   expectedVersion: expectedVersion,
					   currentVersion: current.Value);
			   }
			   _clientes[key] = cliente;
			   _versions[key] = cliente.Version;
			   return Task.CompletedTask;
		   }


		   public Task DeleteAsync(EmpresaId empresaId, Guid clienteId)
		   {
			   if (empresaId is null) throw new ArgumentNullException(nameof(empresaId));
			   var key = (empresaId.Valor, clienteId);
			   _clientes.Remove(key);
			   _versions.Remove(key);
			   return Task.CompletedTask;
		   }


		   public Task DeleteManyAsync(EmpresaId empresaId, IEnumerable<Guid> clienteIds)
		   {
			   if (empresaId is null) throw new ArgumentNullException(nameof(empresaId));
			   foreach (var id in clienteIds)
			   {
				   var key = (empresaId.Valor, id);
				   _clientes.Remove(key);
				   _versions.Remove(key);
			   }
			   return Task.CompletedTask;
		   }


		   public Task DeleteAllAsync(EmpresaId empresaId)
		   {
			   if (empresaId is null) throw new ArgumentNullException(nameof(empresaId));
			   var keysToRemove = _clientes.Keys.Where(k => k.EmpresaId == empresaId.Valor).ToList();
			   foreach (var key in keysToRemove)
			   {
				   _clientes.Remove(key);
				   _versions.Remove(key);
			   }
			   return Task.CompletedTask;
		   }


		   public Task<IReadOnlyList<Cliente>> SearchAsync(EmpresaId empresaId, string? filtro, int? skip = null, int? take = null)
		   {
			   if (empresaId is null) throw new ArgumentNullException(nameof(empresaId));
			   IEnumerable<Cliente> query = _clientes.Values.Where(c => c.EmpresaId == empresaId);
			   if (!string.IsNullOrWhiteSpace(filtro))
			   {
				   query = query.Where(c =>
					   (c.Documento.Numero != null && c.Documento.Numero.Contains(filtro, StringComparison.OrdinalIgnoreCase)) ||
					   (c.RazonSocial != null && c.RazonSocial.Valor != null && c.RazonSocial.Valor.Contains(filtro, StringComparison.OrdinalIgnoreCase)) ||
					   (c.Nombres != null && c.Nombres.Completo != null && c.Nombres.Completo.Contains(filtro, StringComparison.OrdinalIgnoreCase))
				   );
			   }
			   if (skip.HasValue) query = query.Skip(skip.Value);
			   if (take.HasValue) query = query.Take(take.Value);
			   return Task.FromResult((IReadOnlyList<Cliente>)query.ToList());
		   }
	}
}

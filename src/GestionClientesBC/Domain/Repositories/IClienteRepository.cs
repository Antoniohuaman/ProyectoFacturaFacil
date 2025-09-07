using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestionClientesBC.Domain.Aggregates;

namespace GestionClientesBC.Domain.Repositories
{
	/// <summary>
	/// Repositorio de clientes (contrato DDD). Permite operaciones CRUD, búsqueda y eliminación flexible.
	/// </summary>
	public interface IClienteRepository
	{
		// Obtiene un cliente por su Id
		Task<Cliente?> GetByIdAsync(Guid clienteId);

		// Obtiene todos los clientes (paginado opcional)
		Task<IReadOnlyList<Cliente>> GetAllAsync(int? skip = null, int? take = null);

		// Agrega un nuevo cliente
		Task AddAsync(Cliente cliente);

		// Actualiza un cliente existente
		Task UpdateAsync(Cliente cliente);

		// Elimina un cliente por Id (soft o hard delete según implementación)
		Task DeleteAsync(Guid clienteId);

		// Elimina una lista de clientes por sus Ids
		Task DeleteManyAsync(IEnumerable<Guid> clienteIds);

		// Elimina todos los clientes (operación masiva, usar con precaución)
		Task DeleteAllAsync();

		// Búsqueda flexible (por ejemplo, por documento, nombre, etc.)
		Task<IReadOnlyList<Cliente>> SearchAsync(string? filtro, int? skip = null, int? take = null);
	}
}

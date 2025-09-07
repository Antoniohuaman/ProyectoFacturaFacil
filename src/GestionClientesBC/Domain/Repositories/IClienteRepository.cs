using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestionClientesBC.Domain.Aggregates;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Domain.Repositories
{
	/// <summary>
	/// Repositorio de clientes (contrato DDD). Permite operaciones CRUD, búsqueda y eliminación flexible.
	/// </summary>
	public interface IClienteRepository
	{
		// Obtiene un cliente por su Id y empresa
		Task<Cliente?> GetByIdAsync(EmpresaId empresaId, Guid clienteId);

		// Obtiene todos los clientes de una empresa (paginado opcional)
		Task<IReadOnlyList<Cliente>> GetAllAsync(EmpresaId empresaId, int? skip = null, int? take = null);

		// Agrega un nuevo cliente
		Task AddAsync(Cliente cliente);

		// Actualiza un cliente existente
		Task UpdateAsync(Cliente cliente);

		// Elimina un cliente por Id y empresa
		Task DeleteAsync(EmpresaId empresaId, Guid clienteId);

		// Elimina una lista de clientes por sus Ids y empresa
		Task DeleteManyAsync(EmpresaId empresaId, IEnumerable<Guid> clienteIds);

		// Elimina todos los clientes de una empresa (operación masiva, usar con precaución)
		Task DeleteAllAsync(EmpresaId empresaId);

		// Búsqueda flexible (por ejemplo, por documento, nombre, etc.) dentro de una empresa
		Task<IReadOnlyList<Cliente>> SearchAsync(EmpresaId empresaId, string? filtro, int? skip = null, int? take = null);
	}
}

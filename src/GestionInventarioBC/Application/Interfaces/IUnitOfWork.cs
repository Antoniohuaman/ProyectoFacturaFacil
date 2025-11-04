using System.Threading;
using System.Threading.Tasks;

namespace GestionInventarioBC.Application.Interfaces
{
	/// <summary>
	/// Unidad de trabajo para este BC. Asegura consistencia al persistir cambios.
	/// Implementación concreta en Adapters.
	/// </summary>
	public interface IUnitOfWork
	{
		/// <summary>Persiste todos los cambios pendientes de la transacción actual.</summary>
		Task CommitAsync(CancellationToken ct = default);

		/// <summary>Alias común en otros BCs.</summary>
		Task SaveChangesAsync(CancellationToken ct = default) => CommitAsync(ct);
	}
}


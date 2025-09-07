using System.Threading;
using System.Threading.Tasks;

namespace GestionClientesBC.Domain.Repositories
{
	/// <summary>
	/// Contrato de unidad de trabajo para el BC de Gestión de Clientes.
	/// Permite coordinar transacciones y persistencia atómica.
	/// </summary>
	public interface IUnitOfWork
	{
		/// <summary>
		/// Persiste todos los cambios pendientes en una transacción atómica.
		/// </summary>
		/// <param name="cancellationToken">Token de cancelación opcional.</param>
		Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
	}
}

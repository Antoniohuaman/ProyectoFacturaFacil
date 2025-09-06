using System.Threading.Tasks;

namespace CatalogoArticulosBC.Domain.Repositories
{
	/// <summary>
	/// Abstracción de unidad de trabajo para operaciones transaccionales en el dominio de catálogo de artículos.
	/// </summary>
	public interface IUnitOfWork
	{
		/// <summary>
		/// Confirma todos los cambios realizados en la unidad de trabajo.
		/// </summary>
		Task CommitAsync();

		/// <summary>
		/// Revierte los cambios realizados en la unidad de trabajo.
		/// </summary>
		Task RollbackAsync();
	}
}

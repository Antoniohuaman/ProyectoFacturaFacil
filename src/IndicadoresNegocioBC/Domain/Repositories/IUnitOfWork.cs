using System;
using System.Threading;
using System.Threading.Tasks;

namespace IndicadoresNegocioBC.Domain.Repositories
{
	/// <summary>
	/// Contrato de Unidad de Trabajo para el BC de Indicadores de Negocio.
	/// Coordina la persistencia atómica de los agregados y expone los repositorios del dominio.
	/// </summary>
	public interface IUnitOfWork : IDisposable
	{
		IIndicadorNegocioRepository IndicadorNegocioRepository { get; }
		INotificacionIndicadorRepository NotificacionIndicadorRepository { get; }

		/// <summary>
		/// Persiste todos los cambios pendientes de la unidad de trabajo de forma atómica.
		/// </summary>
		/// <param name="cancellationToken">Token de cancelación.</param>
		/// <returns>Número de entidades afectadas.</returns>
		Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Persiste todos los cambios pendientes de la unidad de trabajo de forma atómica (sincrónico).
		/// </summary>
		/// <returns>Número de entidades afectadas.</returns>
		int SaveChanges();
	}
}

using System.Threading;
using System.Threading.Tasks;
using ComprobantesElectronicosBC.Application.UseCases;

namespace ComprobantesElectronicosBC.Application.Interfaces
{
	/// <summary>
	/// Persiste los datos de un comprobante emitido desde la capa de aplicación.
	/// Implementa la infraestructura en Adapters/Output/Persistence.
	/// </summary>
	public interface IComprobanteEmitidoPersister
	{
		/// <summary>
		/// Guarda los datos de un comprobante emitido y devuelve el resultado de persistencia.
		/// </summary>
		/// <param name="data">DTO con los datos del comprobante emitido.</param>
		/// <param name="ct">Token de cancelación.</param>
		/// <returns>Resultado de persistencia con el Id y versión.</returns>
		Task<ComprobantePersistido> GuardarEmitidoAsync(
		EmitirComprobanteUseCase.ComprobanteParaEmitir data,
		CancellationToken ct = default);
	}

	/// <summary>
	/// Resultado de persistencia para comprobantes emitidos.
	/// </summary>
	public sealed record ComprobantePersistido(System.Guid Id, int Version);
}

using System.Threading;
using System.Threading.Tasks;
using SharedKernel.ValueObjects;

namespace ComprobantesElectronicosBC.Application.Interfaces
{
	/// <summary>
	/// Servicio para reservar la siguiente numeración de comprobante.
	/// </summary>
	public interface INumeracionService
	{
		/// <summary>
		/// Reserva la siguiente serie y número disponible para un comprobante.
		/// </summary>
		/// <param name="empresaId">Identificador de la empresa.</param>
		/// <param name="establecimientoId">Identificador del establecimiento.</param>
		/// <param name="tipoComprobante">Tipo de comprobante (FACTURA, BOLETA, etc.).</param>
		/// <param name="seriePreferida">Serie preferida (opcional).</param>
		/// <param name="ct">Token de cancelación.</param>
		/// <returns>DTO con la serie y número reservado, o null si no hay disponible.</returns>
		Task<SerieNumeroDto?> ReservarSiguienteAsync(
			EmpresaId empresaId,
			EstablecimientoId establecimientoId,
			string tipoComprobante,
			string? seriePreferida,
			CancellationToken ct = default);
	}

	/// <summary>
	/// DTO para devolver la serie y número reservado.
	/// </summary>
	public sealed class SerieNumeroDto
	{
		public string Serie { get; init; } = string.Empty;
		public int Numero { get; init; }
	}
}

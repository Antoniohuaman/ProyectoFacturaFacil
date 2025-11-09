using System.Threading;
using System.Threading.Tasks;
using ComprobantesElectronicosBC.Application.UseCases;
using ComprobantesElectronicosBC.Domain.Mappers;

namespace ComprobantesElectronicosBC.Application.Interfaces
{
	/// <summary>
	/// Persiste los datos de un comprobante emitido desde la capa de aplicación.
	/// Implementa la infraestructura en Adapters/Output/Persistence.
	/// </summary>
	public interface IComprobanteEmitidoPersister
	{
		/// <summary>
		/// Nuevo overload: persiste a partir del snapshot generado desde el agregado (sin recálculo en Application).
		/// </summary>
		Task<ComprobantePersistido> GuardarEmitidoAsync(
			ComprobanteEmitidoSnapshot snapshot,
			CancellationToken ct = default);

		/// <summary>
		/// Guarda los datos de un comprobante emitido y devuelve el resultado de persistencia.
		/// </summary>
		/// <param name="data">DTO con los datos del comprobante emitido.</param>
		/// <param name="ct">Token de cancelación.</param>
		/// <returns>Resultado de persistencia con el Id y versión.</returns>
		[System.Obsolete("Método legacy pre-refactor. Usa el snapshot. Delegará internamente mientras exista compatibilidad.")]
		Task<ComprobantePersistido> GuardarEmitidoAsync(object legacyData, CancellationToken ct = default)
		{
			// Delegación mínima: si el caller pasa ya un snapshot lo usamos directamente, de lo contrario intentamos mapear
			if (legacyData is ComprobanteEmitidoSnapshot snap)
				return GuardarEmitidoAsync(snap, ct);

			// Intento de reflexión básica para compatibilidad temporal: buscamos propiedades clave.
			var tipo = legacyData.GetType();
			var monedaProp = tipo.GetProperty("Moneda") ?? tipo.GetProperty("moneda");
			if (monedaProp == null)
				throw new System.NotSupportedException("Legacy DTO sin propiedad Moneda; actualiza a snapshot.");
			var moneda = monedaProp.GetValue(legacyData) as SharedKernel.ValueObjects.Moneda;
			if (moneda == null)
				throw new System.NotSupportedException("Legacy DTO Moneda inválida; actualiza a snapshot.");

			// No reconstruimos totales; se fuerza actualización a nuevo modelo.
			throw new System.NotSupportedException("Legacy DTO soportado parcialmente: requiere migración completa al snapshot.");
		}
	}

	/// <summary>
	/// Resultado de persistencia para comprobantes emitidos.
	/// </summary>
	public sealed record ComprobantePersistido(System.Guid Id, int Version);
}

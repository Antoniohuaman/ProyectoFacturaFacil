using ComprobantesElectronicosBC.Domain.ValueObjects;
using System;

namespace ComprobantesElectronicosBC.Domain.Specifications
{
	/// <summary>
	/// Valida que el TipoDeComprobante sea válido y soportado por la solución.
	/// Solo se permite "01" (Factura) y "03" (Boleta).
	/// Feedback detallado en caso de error.
	/// </summary>
	public static class SpecTipoDeComprobanteValido
	{
		/// <summary>
		/// Valida que el tipo de comprobante sea Factura o Boleta.
		/// </summary>
		/// <param name="tipo">VO TipoDeComprobante</param>
		/// <returns>ValidationResult con feedback detallado</returns>
		public static ValidationResult IsSatisfiedBy(TipoDeComprobante? tipo)
		{
			if (tipo is null)
				return ValidationResult.Failure("El tipo de comprobante es obligatorio.");

			if (tipo.EsFactura || tipo.EsBoleta)
				return ValidationResult.Success();

			return ValidationResult.Failure($"Tipo de comprobante '{tipo.Codigo}' no soportado. Solo se permite Factura ('01') o Boleta ('03').");
		}
	}
	// Se reutiliza la clase ValidationResult definida en otra Specification.
}

using ComprobantesElectronicosBC.Domain.ValueObjects;
using System;

namespace ComprobantesElectronicosBC.Domain.Specifications
{
	/// <summary>
	/// Valida que la SerieYNumero sea compatible con el TipoDeComprobante.
	/// - Factura ("01") requiere serie que inicia con "F"
	/// - Boleta  ("03") requiere serie que inicia con "B"
	/// Otros tipos: no imponen restricción de prefijo.
	/// Feedback detallado en caso de incompatibilidad o formato inválido.
	/// </summary>
	public static class SpecSerieYNumeroCompatibleConTipo
	{
		/// <summary>
		/// Valida compatibilidad entre SerieYNumero y TipoDeComprobante.
		/// </summary>
		/// <param name="serieNumero">VO SerieYNumero</param>
		/// <param name="tipo">VO TipoDeComprobante</param>
		/// <returns>ValidationResult con feedback detallado</returns>
		public static ValidationResult IsSatisfiedBy(SerieYNumero? serieNumero, TipoDeComprobante? tipo)
		{
			if (serieNumero is null)
				return ValidationResult.Failure("La serie y número del comprobante son obligatorios.");
			if (tipo is null)
				return ValidationResult.Failure("El tipo de comprobante es obligatorio.");

			// Validación de formato de serie
			var serie = serieNumero.Serie;
			if (string.IsNullOrWhiteSpace(serie) || serie.Length < 1 || serie.Length > 4)
				return ValidationResult.Failure("La serie debe tener entre 1 y 4 caracteres alfanuméricos.");
			foreach (var ch in serie)
			{
				var esAlfaNum = (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9');
				if (!esAlfaNum)
					return ValidationResult.Failure("La serie solo admite caracteres alfanuméricos (A–Z, 0–9).");
			}

			// Validación de compatibilidad de prefijo
			if (tipo.EsFactura && !serie.StartsWith("F", StringComparison.Ordinal))
				return ValidationResult.Failure($"La serie '{serie}' no es válida para Factura (debe iniciar con 'F').");
			if (tipo.EsBoleta && !serie.StartsWith("B", StringComparison.Ordinal))
				return ValidationResult.Failure($"La serie '{serie}' no es válida para Boleta (debe iniciar con 'B').");

			// Validación de número
			if (serieNumero.Numero < 1 || serieNumero.Numero > 99_999_999)
				return ValidationResult.Failure("El número debe estar entre 1 y 99,999,999.");

			return ValidationResult.Success();
		}
	}
	// Se reutiliza la clase ValidationResult definida en otra Specification.
}

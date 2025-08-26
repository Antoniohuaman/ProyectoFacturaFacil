using System;

namespace ComprobantesElectronicosBC.Domain.Specifications
{
	/// <summary>
	/// Especificación para validar si la fecha de emisión retroactiva es permitida según reglas SUNAT y negocio.
	/// </summary>
	public sealed class SpecFechaEmisionRetroactiva
	{
		/// <summary>
		/// Valida si la fecha de emisión retroactiva es válida.
		/// </summary>
		/// <param name="fechaEmision">Fecha de emisión propuesta</param>
		/// <param name="fechaActual">Fecha actual del sistema</param>
		/// <returns>Resultado de la validación con feedback detallado.</returns>
		public static ValidationResult IsSatisfiedBy(DateTime fechaEmision, DateTime fechaActual)
		{
			// SUNAT suele permitir hasta 5 días calendario retroactivos
			const int MAX_DIAS_RETROACTIVOS = 5;

			if (fechaEmision.Date > fechaActual.Date)
				return ValidationResult.Failure("La fecha de emisión no puede ser futura.");

			var diasRetroactivos = (fechaActual.Date - fechaEmision.Date).Days;
			if (diasRetroactivos > MAX_DIAS_RETROACTIVOS)
				return ValidationResult.Failure($"La fecha de emisión retroactiva excede el máximo permitido de {MAX_DIAS_RETROACTIVOS} días. Días retroactivos: {diasRetroactivos}");

			if (diasRetroactivos < 0)
				return ValidationResult.Failure("La fecha de emisión no puede ser posterior a la fecha actual.");

			return ValidationResult.Success();
		}
	}
	// Se reutiliza la clase ValidationResult definida en otra Specification.
}

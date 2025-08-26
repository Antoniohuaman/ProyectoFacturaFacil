using System;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Domain.Specifications
{
	/// <summary>
	/// Especificación para validar si el descuento de línea es aplicable en el comprobante.
	/// </summary>
	public sealed class SpecDescuentoLineaAplicable
	{
		/// <summary>
		/// Valida si el descuento de línea es aplicable según reglas de negocio.
		/// </summary>
		/// <param name="descuento">ValueObject DescuentoLinea</param>
		/// <param name="baseAntes">Base antes del descuento</param>
		/// <returns>Resultado de la validación con feedback detallado.</returns>
		public static ValidationResult IsSatisfiedBy(DescuentoLinea descuento, decimal baseAntes)
		{
			if (descuento.EsNinguno)
				return ValidationResult.Success(); // No hay descuento, siempre válido

			if (baseAntes <= 0)
				return ValidationResult.Failure("La base para aplicar el descuento de línea debe ser mayor a cero.");

			if (descuento.Valor <= 0)
				return ValidationResult.Failure("El valor del descuento de línea debe ser mayor a cero.");

			// No permitir que el descuento supere la base
			var montoDescuento = descuento.CalcularMontoSobreBase(baseAntes);
			if (montoDescuento > baseAntes)
				return ValidationResult.Failure("El monto del descuento de línea no puede exceder la base antes del descuento.");

			// Reglas adicionales: porcentaje máximo permitido (ejemplo: 50%)
			if (descuento.EsPorcentaje && descuento.Valor > 50m)
				return ValidationResult.Failure("El porcentaje de descuento de línea no puede exceder el 50%.");

			return ValidationResult.Success();
		}
	}
	// Se reutiliza la clase ValidationResult definida en otra Specification.
}

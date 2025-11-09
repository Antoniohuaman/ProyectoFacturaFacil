using System;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Domain.Specifications
{
	/// <summary>
	/// Especificación para validar si el descuento global es aplicable en el comprobante.
	/// </summary>
	public sealed class SpecDescuentoGlobalAplicable
	{
		/// <summary>
		/// Valida si el descuento global es aplicable según reglas de negocio.
		/// </summary>
		/// <param name="descuento">ValueObject DescuentoGlobal</param>
		/// <param name="subtotalBaseImponible">Subtotal base imponible</param>
		/// <returns>Resultado de la validación con feedback detallado.</returns>
		public static ValidationResult IsSatisfiedBy(DescuentoGlobal descuento, decimal subtotalBaseImponible)
		{
			if (descuento.EsNinguno)
				return ValidationResult.Success(); // No hay descuento, siempre válido

			if (subtotalBaseImponible <= 0)
				return ValidationResult.Failure("El subtotal base imponible debe ser mayor a cero para aplicar descuento global.");

			if (descuento.Valor <= 0)
				return ValidationResult.Failure("El valor del descuento global debe ser mayor a cero.");

			// No permitir que el descuento supere el subtotal
			var montoDescuento = descuento.CalcularMontoDescuento(subtotalBaseImponible);
			if (montoDescuento > subtotalBaseImponible)
				return ValidationResult.Failure("El monto del descuento global no puede exceder el subtotal base imponible.");

			// Regla adicional: el porcentaje debe ser menor que el límite definido (100% no se considera descuento)
			if (descuento.Modo == DescuentoGlobalModo.Porcentaje && descuento.Valor >= DiscountLimits.MaxPercentAllowedExclusive)
				return ValidationResult.Failure($"El porcentaje de descuento global debe ser menor que {DiscountLimits.MaxPercentAllowedExclusive}%.");

			return ValidationResult.Success();
		}
	}
	// Se reutiliza la clase ValidationResult definida en otra Specification.
}

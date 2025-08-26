using System;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Domain.Specifications
{
	/// <summary>
	/// Especificación para validar que la cantidad de un comprobante sea válida según reglas de negocio.
	/// </summary>
	public sealed class SpecCantidadValida
	{
		/// <summary>
		/// Valida que la cantidad sea positiva y cumpla reglas adicionales.
		/// </summary>
		/// <param name="cantidad">ValueObject Cantidad</param>
		/// <returns>Resultado de la validación con feedback detallado.</returns>
		public static ValidationResult IsSatisfiedBy(Cantidad cantidad)
		{
			// Cantidad es un struct, no puede ser null. Validar valor mínimo y máximo.
			if (cantidad.Value <= 0)
				return ValidationResult.Failure($"La cantidad debe ser mayor a cero. Valor recibido: {cantidad.Value}");

			// Reglas adicionales de negocio (ejemplo: máximo permitido)
			const decimal MAX_CANTIDAD = 1000000m;
			if (cantidad.Value > MAX_CANTIDAD)
				return ValidationResult.Failure($"La cantidad excede el máximo permitido ({MAX_CANTIDAD}). Valor recibido: {cantidad.Value}");

			return ValidationResult.Success();
		}
	}

	/// <summary>
	/// Resultado de validación con feedback.
	/// </summary>
	public sealed class ValidationResult
	{
		public bool IsValid { get; }
		public string? ErrorMessage { get; }

		private ValidationResult(bool isValid, string? errorMessage)
		{
			IsValid = isValid;
			ErrorMessage = errorMessage;
		}

		public static ValidationResult Success() => new ValidationResult(true, null);
		public static ValidationResult Failure(string error) => new ValidationResult(false, error);
	}
}

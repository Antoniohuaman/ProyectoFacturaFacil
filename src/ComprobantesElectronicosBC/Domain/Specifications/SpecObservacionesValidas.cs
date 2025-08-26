using System.Text.RegularExpressions;

namespace ComprobantesElectronicosBC.Domain.Specifications
{
	public static class SpecObservacionesValidas
	{
		/// <summary>
		/// Valida que las observaciones sean coherentes y cumplan reglas de negocio.
		/// </summary>
		/// <param name="observaciones">Texto de observaciones (string)</param>
		/// <returns>ValidationResult con feedback detallado</returns>
		public static ValidationResult IsSatisfiedBy(string? observaciones)
		{
			if (string.IsNullOrWhiteSpace(observaciones))
				return ValidationResult.Success(); // Observaciones son opcionales

			if (observaciones.Length < 3)
				return ValidationResult.Failure("Las observaciones deben tener al menos 3 caracteres si se especifican.");

			if (observaciones.Length > 500)
				return ValidationResult.Failure("Las observaciones no pueden exceder 500 caracteres.");


			// No caracteres prohibidos (solo texto, números, puntuación básica)
			var regex = new Regex(@"^[A-Za-z0-9 áéíóúÁÉÍÓÚ.,;:()\-_'""\n\r]+$");
			if (!regex.IsMatch(observaciones))
				return ValidationResult.Failure("Las observaciones contienen caracteres no permitidos.");

			return ValidationResult.Success();
		}
	}

	// Se reutiliza la clase ValidationResult definida en otra Specification.
}

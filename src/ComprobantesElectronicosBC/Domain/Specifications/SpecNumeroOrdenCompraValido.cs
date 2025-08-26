using System.Text.RegularExpressions;

namespace ComprobantesElectronicosBC.Domain.Specifications
{
	public static class SpecNumeroOrdenCompraValido
	{
		/// <summary>
		/// Valida que el número de orden de compra cumpla formato y reglas de negocio.
		/// </summary>
		/// <param name="numeroOrdenCompra">Número de orden de compra (string)</param>
		/// <returns>ValidationResult con feedback detallado</returns>
		public static ValidationResult IsSatisfiedBy(string numeroOrdenCompra)
		{
			if (string.IsNullOrWhiteSpace(numeroOrdenCompra))
				return ValidationResult.Failure("El número de orden de compra es obligatorio.");

			// Reglas típicas: alfanumérico, 4-20 caracteres, sin caracteres especiales prohibidos
			if (numeroOrdenCompra.Length < 4 || numeroOrdenCompra.Length > 20)
				return ValidationResult.Failure("El número de orden de compra debe tener entre 4 y 20 caracteres.");

			var regex = new Regex(@"^[A-Za-z0-9\-_.]+$");
			if (!regex.IsMatch(numeroOrdenCompra))
				return ValidationResult.Failure("El número de orden de compra solo puede contener letras, números, guiones, guiones bajos y puntos.");

			return ValidationResult.Success();
		}
	}

	// Se reutiliza la clase ValidationResult definida en otra Specification.
}

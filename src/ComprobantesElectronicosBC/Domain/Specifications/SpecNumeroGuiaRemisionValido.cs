using System.Text.RegularExpressions;

namespace ComprobantesElectronicosBC.Domain.Specifications
{
	public static class SpecNumeroGuiaRemisionValido
	{
		/// <summary>
		/// Valida que el número de guía de remisión cumpla formato y reglas SUNAT.
		/// </summary>
		/// <param name="numeroGuia">Número de guía de remisión (string)</param>
		/// <returns>ValidationResult con feedback detallado</returns>
		public static ValidationResult IsSatisfiedBy(string numeroGuia)
		{
			if (string.IsNullOrWhiteSpace(numeroGuia))
				return ValidationResult.Failure("El número de guía de remisión es obligatorio.");

			// Formato típico: RUC-serie-correlativo, ej: 20123456789-001-00012345
			var regex = new Regex(@"^(\d{11})-(\d{3})-(\d{1,8})$");
			if (!regex.IsMatch(numeroGuia))
				return ValidationResult.Failure("El número de guía de remisión debe tener el formato RUC-serie-correlativo, ej: 20123456789-001-00012345.");

			var parts = numeroGuia.Split('-');
			if (parts.Length != 3)
				return ValidationResult.Failure("El número de guía de remisión debe tener tres partes separadas por guiones.");

			if (parts[0].Length != 11)
				return ValidationResult.Failure("El RUC en la guía de remisión debe tener 11 dígitos.");

			if (parts[1].Length != 3)
				return ValidationResult.Failure("La serie en la guía de remisión debe tener 3 dígitos.");

			if (parts[2].Length < 1 || parts[2].Length > 8)
				return ValidationResult.Failure("El correlativo en la guía de remisión debe tener entre 1 y 8 dígitos.");

			return ValidationResult.Success();
		}
	}

	// Se reutiliza la clase ValidationResult definida en otra Specification.
}

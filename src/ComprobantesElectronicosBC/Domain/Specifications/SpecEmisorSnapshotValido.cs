using System;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Domain.Specifications
{
	/// <summary>
	/// Especificación para validar que el snapshot del emisor sea válido según reglas de negocio.
	/// </summary>
	public sealed class SpecEmisorSnapshotValido
	{
		/// <summary>
		/// Valida que el emisor tenga RUC, razón social y dirección válidos.
		/// </summary>
		/// <param name="emisor">ValueObject EmisorSnapshot</param>
		/// <returns>Resultado de la validación con feedback detallado.</returns>
		public static ValidationResult IsSatisfiedBy(EmisorSnapshot emisor)
		{
			if (emisor is null)
				return ValidationResult.Failure("El emisor no puede ser nulo.");

			if (string.IsNullOrWhiteSpace(emisor.Ruc))
				return ValidationResult.Failure("El RUC del emisor es obligatorio.");

			if (emisor.Ruc.Length != 11 || !long.TryParse(emisor.Ruc, out _))
				return ValidationResult.Failure("El RUC del emisor debe tener 11 dígitos numéricos.");

			if (string.IsNullOrWhiteSpace(emisor.RazonSocial))
				return ValidationResult.Failure("La razón social del emisor es obligatoria.");

			if (emisor.Domicilio is null)
				return ValidationResult.Failure("El domicilio fiscal del emisor es obligatorio.");

			// Opcional: validar nombre comercial si existe
			if (emisor.NombreComercial != null && emisor.NombreComercial.Length > 100)
				return ValidationResult.Failure("El nombre comercial del emisor no puede exceder 100 caracteres.");

			// Opcional: validar email si existe
			if (emisor.Email != null && string.IsNullOrWhiteSpace(emisor.Email.Value))
				return ValidationResult.Failure("El email del emisor, si se especifica, no puede estar vacío.");

			return ValidationResult.Success();
		}
	}
	// Se reutiliza la clase ValidationResult definida en otra Specification.
}

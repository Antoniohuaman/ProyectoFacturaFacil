using System;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Domain.Specifications
{
	/// <summary>
	/// Especificación para validar que el snapshot de cliente sea válido según reglas de negocio.
	/// </summary>
	public sealed class SpecClienteSnapshotValido
	{
		/// <summary>
		/// Valida que el cliente tenga documento y nombre válidos.
		/// </summary>
		/// <param name="cliente">ValueObject ClienteSnapshot</param>
		/// <returns>Resultado de la validación con feedback detallado.</returns>
		public static ValidationResult IsSatisfiedBy(ClienteSnapshot cliente)
		{
			if (cliente is null)
				return ValidationResult.Failure("El cliente no puede ser nulo.");

			if (cliente.Documento is null)
				return ValidationResult.Failure("El documento de identidad del cliente es obligatorio.");

			if (string.IsNullOrWhiteSpace(cliente.Documento.Numero))
				return ValidationResult.Failure("El número de documento del cliente es obligatorio.");

			if (string.IsNullOrWhiteSpace(cliente.Nombre))
				return ValidationResult.Failure("El nombre del cliente es obligatorio.");

			// Reglas adicionales: si es RUC, validar longitud y formato
			if (cliente.EsRuc && cliente.Documento.Numero.Length != 11)
				return ValidationResult.Failure("El RUC debe tener 11 dígitos.");

			return ValidationResult.Success();
		}
	}

	// Se reutiliza la clase ValidationResult definida en otra Specification.
}

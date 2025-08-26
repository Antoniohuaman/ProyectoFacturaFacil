using System;
using System.Text.RegularExpressions;

namespace ComprobantesElectronicosBC.Domain.Specifications
{
	/// <summary>
	/// Especificación para validar la descripción de producto en comprobantes electrónicos.
	/// </summary>
	public sealed class SpecDescripcionProductoValida
	{
		/// <summary>
		/// Valida que la descripción de producto cumpla reglas de negocio y coherencia.
		/// </summary>
		/// <param name="descripcion">Texto de la descripción</param>
		/// <returns>Resultado de la validación con feedback detallado.</returns>
		public static ValidationResult IsSatisfiedBy(string descripcion)
		{
			if (string.IsNullOrWhiteSpace(descripcion))
				return ValidationResult.Failure("La descripción del producto es obligatoria.");

			// Longitud mínima y máxima (SUNAT suele exigir entre 3 y 250 caracteres)
			if (descripcion.Length < 3)
				return ValidationResult.Failure("La descripción del producto debe tener al menos 3 caracteres.");

			if (descripcion.Length > 250)
				return ValidationResult.Failure("La descripción del producto excede el máximo permitido de 250 caracteres.");

			// No permitir solo caracteres especiales o números
			if (!Regex.IsMatch(descripcion, @"[a-zA-ZáéíóúÁÉÍÓÚñÑ]"))
				return ValidationResult.Failure("La descripción debe contener al menos una letra.");

			// Reglas adicionales: evitar palabras prohibidas o incoherentes
			string[] palabrasProhibidas = { "N/A", "SIN DESCRIPCION", "NO DEFINIDO" };
			foreach (var prohibida in palabrasProhibidas)
			{
				if (descripcion.Trim().ToUpperInvariant().Contains(prohibida))
					return ValidationResult.Failure($"La descripción contiene la palabra prohibida: '{prohibida}'.");
			}

			return ValidationResult.Success();
		}
	}
	// Se reutiliza la clase ValidationResult definida en otra Specification.
}

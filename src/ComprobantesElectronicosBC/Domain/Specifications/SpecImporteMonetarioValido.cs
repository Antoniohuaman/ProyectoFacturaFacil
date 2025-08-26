using SharedKernel.ValueObjects;

namespace ComprobantesElectronicosBC.Domain.Specifications
{
	public static class SpecImporteMonetarioValido
	{
		/// <summary>
		/// Valida que el importe monetario (Dinero) sea coherente y cumpla reglas de negocio.
		/// </summary>
		/// <param name="importe">ValueObject Dinero</param>
		/// <param name="minimo">Valor mínimo permitido (opcional, default 0.01)</param>
		/// <param name="maximo">Valor máximo permitido (opcional, default 99999999.99)</param>
		/// <returns>ValidationResult con feedback detallado</returns>
		public static ValidationResult IsSatisfiedBy(Dinero importe, decimal minimo = 0.01m, decimal maximo = 99999999.99m)
		{
			if (importe is null)
				return ValidationResult.Failure("El importe monetario no puede ser nulo.");

			if (importe.Moneda is null)
				return ValidationResult.Failure("La moneda del importe monetario no puede ser nula.");

			if (string.IsNullOrWhiteSpace(importe.Moneda.Codigo))
				return ValidationResult.Failure("El código de moneda es obligatorio.");

			if (importe.Monto < minimo)
				return ValidationResult.Failure($"El importe monetario debe ser mayor o igual a {minimo:N2}. Valor recibido: {importe.Monto:N2}.");

			if (importe.Monto > maximo)
				return ValidationResult.Failure($"El importe monetario excede el máximo permitido de {maximo:N2}. Valor recibido: {importe.Monto:N2}.");

			if (importe.Moneda.Decimales < 0 || importe.Moneda.Decimales > 4)
				return ValidationResult.Failure($"El número de decimales de la moneda ({importe.Moneda.Decimales}) debe estar entre 0 y 4.");

			// Validación de redondeo
			var montoRedondeado = Math.Round(importe.Monto, importe.Moneda.Decimales);
			if (importe.Monto != montoRedondeado)
				return ValidationResult.Failure($"El importe monetario debe estar redondeado a {importe.Moneda.Decimales} decimales. Valor recibido: {importe.Monto:N4}.");

			return ValidationResult.Success();
		}
	}

	// Se reutiliza la clase ValidationResult definida en otra Specification.
}

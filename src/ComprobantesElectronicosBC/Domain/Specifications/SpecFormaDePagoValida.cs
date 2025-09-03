using SharedKernel.ValueObjects;

namespace ComprobantesElectronicosBC.Domain.Specifications
{
	public static class SpecFormaDePagoValida
	{
		/// <summary>
		/// Valida que la forma de pago sea coherente y cumpla reglas de negocio.
		/// </summary>
		/// <param name="formaDePago">ValueObject FormaDePago</param>
		/// <returns>ValidationResult con feedback detallado</returns>
		public static ValidationResult IsSatisfiedBy(FormaDePago formaDePago)
		{
			if (formaDePago is null)
				return ValidationResult.Failure("La forma de pago no puede ser nula.");

			if (string.IsNullOrWhiteSpace(formaDePago.PaymentMeansCode))
				return ValidationResult.Failure("El código de medio de pago es obligatorio.");

			if (formaDePago.PaymentMeansCode != FormaDePago.CONTADO && formaDePago.PaymentMeansCode != FormaDePago.CREDITO)
				return ValidationResult.Failure($"El código de medio de pago '{formaDePago.PaymentMeansCode}' no es válido. Solo se permite CONTADO ('10') o CRÉDITO ('20').");

			if (formaDePago.EsCredito && string.IsNullOrWhiteSpace(formaDePago.MetodoCodigo))
				return ValidationResult.Failure("Para CRÉDITO, el método de pago debe estar especificado.");

			if (formaDePago.EsContado && string.IsNullOrWhiteSpace(formaDePago.MetodoCodigo))
				return ValidationResult.Failure("Para CONTADO, el método de pago debe estar especificado.");

			if (!string.IsNullOrWhiteSpace(formaDePago.MetodoNombre) && formaDePago.MetodoNombre.Length > 100)
				return ValidationResult.Failure("El nombre del método de pago no puede exceder 100 caracteres.");

			return ValidationResult.Success();
		}
	}

	// Se reutiliza la clase ValidationResult definida en otra Specification.
}

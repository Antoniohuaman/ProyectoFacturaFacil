using System;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Domain.Specifications
{
	public static class SpecFechaVencimientoValida
	{
		/// <summary>
		/// Valida que la fecha de vencimiento sea coherente respecto a la fecha de emisión y reglas de negocio.
		/// </summary>
		/// <param name="fechaVencimiento">ValueObject FechaVencimiento</param>
		/// <param name="fechaEmision">Fecha de emisión (DateOnly)</param>
		/// <param name="maxDiasCredito">Máximo permitido de días de crédito (opcional, default 365)</param>
		/// <returns>ValidationResult con feedback detallado</returns>
		public static ValidationResult IsSatisfiedBy(FechaVencimiento fechaVencimiento, DateOnly fechaEmision, int maxDiasCredito = 365)
		{
			if (fechaVencimiento is null)
				return ValidationResult.Failure("La fecha de vencimiento no puede ser nula.");

			if (fechaVencimiento.Value < fechaEmision)
				return ValidationResult.Failure($"La fecha de vencimiento ({fechaVencimiento.Value:yyyy-MM-dd}) no puede ser anterior a la fecha de emisión ({fechaEmision:yyyy-MM-dd}).");

			var hoy = DateOnly.FromDateTime(DateTime.Now);
			if (fechaVencimiento.Value < hoy)
				return ValidationResult.Failure($"La fecha de vencimiento ({fechaVencimiento.Value:yyyy-MM-dd}) no puede ser anterior a la fecha actual ({hoy:yyyy-MM-dd}).");

			var diasCredito = (fechaVencimiento.Value.ToDateTime(TimeOnly.MinValue) - fechaEmision.ToDateTime(TimeOnly.MinValue)).Days;
			if (diasCredito > maxDiasCredito)
				return ValidationResult.Failure($"La fecha de vencimiento excede el máximo permitido de {maxDiasCredito} días de crédito. Días calculados: {diasCredito}.");

			if (diasCredito <= 0)
				return ValidationResult.Failure("La fecha de vencimiento debe ser al menos un día posterior a la fecha de emisión para crédito.");

			return ValidationResult.Success();
		}
	}

	// Se reutiliza la clase ValidationResult definida en otra Specification.
}

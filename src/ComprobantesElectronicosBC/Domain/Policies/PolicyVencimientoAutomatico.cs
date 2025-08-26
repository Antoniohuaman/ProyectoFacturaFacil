using System;
using System.Collections.Generic;

namespace ComprobantesElectronicosBC.Domain.Policies
{
	public class PolicyVencimientoAutomatico
	{
		public class Result
		{
			public bool Success { get; }
			public string? ErrorMessage { get; }
			public IReadOnlyList<string>? ValidationErrors { get; }
			public DateTime? FechaVencimiento { get; }

			public Result(bool success, DateTime? fechaVencimiento = null, string? errorMessage = null, IReadOnlyList<string>? validationErrors = null)
			{
				Success = success;
				FechaVencimiento = fechaVencimiento;
				ErrorMessage = errorMessage;
				ValidationErrors = validationErrors;
			}
		}

		public Result CalcularVencimiento(object? comprobante, int? maxDiasCredito = null)
		{
			var errores = new List<string>();
			if (comprobante == null)
			{
				errores.Add("El comprobante no puede ser nulo.");
				return new Result(false, null, "Comprobante nulo.", errores);
			}

			var tipo = comprobante.GetType();
			var fechaEmisionProp = tipo.GetProperty("FechaEmision");
			var diasCreditoProp = tipo.GetProperty("DiasCredito");

			var fechaEmision = fechaEmisionProp?.GetValue(comprobante) as DateTime?;
			var diasCredito = diasCreditoProp?.GetValue(comprobante) as int?;

			if (fechaEmision == null)
				errores.Add("La fecha de emisión no está definida.");
			if (diasCredito == null)
				errores.Add("Los días de crédito no están definidos.");
			else if (diasCredito < 0)
				errores.Add("Los días de crédito no pueden ser negativos.");
			if (maxDiasCredito.HasValue && diasCredito.HasValue && diasCredito.Value > maxDiasCredito.Value)
				errores.Add($"Los días de crédito exceden el máximo permitido ({maxDiasCredito.Value}).");

			if (errores.Count > 0)
				return new Result(false, null, "Validación de vencimiento automático fallida.", errores);

			// Null checks antes de calcular
			if (!fechaEmision.HasValue || !diasCredito.HasValue)
			{
				errores.Add("No se puede calcular la fecha de vencimiento por datos faltantes.");
				return new Result(false, null, "Datos insuficientes para el cálculo.", errores);
			}

			var fechaVencimiento = fechaEmision.Value.AddDays(diasCredito.Value);
			if (fechaVencimiento < fechaEmision.Value)
			{
				errores.Add("La fecha de vencimiento no puede ser anterior a la fecha de emisión.");
				return new Result(false, null, "Fecha de vencimiento inválida.", errores);
			}

			return new Result(true, fechaVencimiento);
		}
	}
}

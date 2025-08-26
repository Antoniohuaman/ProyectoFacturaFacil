using System;
using System.Collections.Generic;

namespace ComprobantesElectronicosBC.Domain.Policies
{
	public class PolicyValidacionImporteMonetario
	{
		public class Result
		{
			public bool Success { get; }
			public string? ErrorMessage { get; }
			public IReadOnlyList<string>? ValidationErrors { get; }

			public Result(bool success, string? errorMessage = null, IReadOnlyList<string>? validationErrors = null)
			{
				Success = success;
				ErrorMessage = errorMessage;
				ValidationErrors = validationErrors;
			}
		}

		public Result Validate(object? importeMonetario, decimal? maximoPermitido = null, string? monedaEsperada = null)
		{
			var errores = new List<string>();
			if (importeMonetario == null)
			{
				errores.Add("El importe monetario no puede ser nulo.");
				return new Result(false, "Importe nulo.", errores);
			}

			var tipo = importeMonetario.GetType();
			var valorProp = tipo.GetProperty("Valor");
			var monedaProp = tipo.GetProperty("Moneda");

			// Valor
			var valor = valorProp?.GetValue(importeMonetario) as decimal?;
			if (valor == null)
				errores.Add("El valor del importe no está definido.");
			else
			{
				if (valor <= 0)
					errores.Add("El importe debe ser mayor que cero.");
				if (maximoPermitido.HasValue && valor > maximoPermitido.Value)
					errores.Add($"El importe excede el máximo permitido ({maximoPermitido.Value}).");
			}

			// Moneda
			var moneda = monedaProp?.GetValue(importeMonetario) as string;
			if (!string.IsNullOrWhiteSpace(monedaEsperada) && moneda != monedaEsperada)
				errores.Add($"La moneda debe ser '{monedaEsperada}'.");

			// Ejemplo: validación de decimales para moneda específica
			if (!string.IsNullOrWhiteSpace(moneda) && valor != null)
			{
				if (moneda == "JPY" && valor % 1 != 0)
					errores.Add("El importe en yenes japoneses (JPY) no puede tener decimales.");
			}

			if (errores.Count > 0)
				return new Result(false, "Validación de importe monetario fallida.", errores);

			return new Result(true);
		}
	}
}

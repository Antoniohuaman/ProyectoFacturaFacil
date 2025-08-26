using System;
using System.Collections.Generic;

namespace ComprobantesElectronicosBC.Domain.Policies
{
	/// <summary>
	/// Política para el control normativo de la forma de pago en comprobantes electrónicos.
	/// </summary>
	public class PolicyFormaDePago
	{
		/// <summary>
		/// Valida que la forma de pago sea permitida y cumpla con las reglas normativas.
		/// </summary>
		/// <param name="formaDePago">Forma de pago (ej: Contado, Crédito, Transferencia, etc).</param>
		/// <param name="diasCredito">Días de crédito (solo aplica si la forma es Crédito).</param>
		/// <returns>Resultado detallado de la validación.</returns>
		public ResultadoValidacionFormaDePago ValidarFormaDePago(string? formaDePago, int? diasCredito = null)
		{
			var permitidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				"Contado", "Crédito", "Transferencia", "Tarjeta", "Cheque"
			};

			if (string.IsNullOrWhiteSpace(formaDePago))
			{
				return new ResultadoValidacionFormaDePago
				{
					EsExitoso = false,
					MotivoFallo = "La forma de pago es nula o vacía."
				};
			}

			if (!permitidas.Contains(formaDePago))
			{
				return new ResultadoValidacionFormaDePago
				{
					EsExitoso = false,
					MotivoFallo = $"La forma de pago '{formaDePago}' no está permitida."
				};
			}

			if (formaDePago.Equals("Crédito", StringComparison.OrdinalIgnoreCase))
			{
				if (diasCredito == null || diasCredito <= 0 || diasCredito > 180)
				{
					return new ResultadoValidacionFormaDePago
					{
						EsExitoso = false,
						MotivoFallo = "Los días de crédito deben estar entre 1 y 180."
					};
				}
			}

			return new ResultadoValidacionFormaDePago
			{
				EsExitoso = true,
				MotivoFallo = null
			};
		}
	}

	public class ResultadoValidacionFormaDePago
	{
		public bool EsExitoso { get; set; }
		public string? MotivoFallo { get; set; }
	}
}

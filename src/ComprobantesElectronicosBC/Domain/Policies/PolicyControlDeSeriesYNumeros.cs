using System;
using System.Collections.Generic;

namespace ComprobantesElectronicosBC.Domain.Policies
{
	/// <summary>
	/// Política para el control normativo de series y números en comprobantes electrónicos.
	/// </summary>
	public class PolicyControlDeSeriesYNumeros
	{
		/// <summary>
		/// Valida que la serie y el número del comprobante cumplan con las reglas normativas.
		/// </summary>
		/// <param name="serie">Serie del comprobante.</param>
		/// <param name="numero">Número del comprobante.</param>
		/// <returns>Resultado detallado de la validación.</returns>
		public ResultadoValidacionSerieNumero ValidarSerieYNumero(string? serie, string? numero)
		{
			if (string.IsNullOrWhiteSpace(serie))
			{
				return new ResultadoValidacionSerieNumero
				{
					EsExitoso = false,
					MotivoFallo = "La serie es nula o vacía."
				};
			}

			if (string.IsNullOrWhiteSpace(numero))
			{
				return new ResultadoValidacionSerieNumero
				{
					EsExitoso = false,
					MotivoFallo = "El número es nulo o vacío."
				};
			}

			// Regla: la serie debe tener exactamente 4 caracteres alfanuméricos
			if (serie.Length != 4 || !EsAlfanumerico(serie))
			{
				return new ResultadoValidacionSerieNumero
				{
					EsExitoso = false,
					MotivoFallo = "La serie debe tener exactamente 4 caracteres alfanuméricos."
				};
			}

			// Regla: el número debe ser numérico y tener entre 1 y 8 dígitos
			if (!int.TryParse(numero, out int num) || numero.Length < 1 || numero.Length > 8)
			{
				return new ResultadoValidacionSerieNumero
				{
					EsExitoso = false,
					MotivoFallo = "El número debe ser numérico y tener entre 1 y 8 dígitos."
				};
			}

			return new ResultadoValidacionSerieNumero
			{
				EsExitoso = true,
				MotivoFallo = null
			};
		}

		private bool EsAlfanumerico(string valor)
		{
			foreach (var c in valor)
			{
				if (!char.IsLetterOrDigit(c))
					return false;
			}
			return true;
		}
	}

	public class ResultadoValidacionSerieNumero
	{
		public bool EsExitoso { get; set; }
		public string? MotivoFallo { get; set; }
	}
}

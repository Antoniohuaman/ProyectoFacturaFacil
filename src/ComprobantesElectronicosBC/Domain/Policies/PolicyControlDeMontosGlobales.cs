using System;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Domain.Policies
{
	/// <summary>
	/// [OBSOLETO] Integrado en ComprobanteElectronico.RecalcularTotales.
	/// </summary>
	[Obsolete("Integrado en Aggregate.RecalcularTotales (montos negativos, tope global y boleta<=700 PEN).")]
	public class PolicyControlDeMontosGlobales
	{
		/// <summary>
		/// Valida que el monto total del comprobante cumpla con las reglas normativas y retorna resultado detallado.
		/// </summary>
		/// <param name="comprobante">Comprobante electrónico a validar.</param>
		/// <returns>Resultado con éxito y motivo.</returns>
	public ResultadoControlMontosGlobales ValidarMontoGlobal(object comprobante)
		{
			if (comprobante == null)
				return ResultadoControlMontosGlobales.Fallo("El comprobante es nulo.");

			// Usar reflection para obtener Total y Tipo
			var tipoProp = comprobante.GetType().GetProperty("Tipo");
			var totalProp = comprobante.GetType().GetProperty("Total");
			if (totalProp == null)
				return ResultadoControlMontosGlobales.Fallo("No se encontró la propiedad Total en el comprobante.");

			var totalValue = totalProp.GetValue(comprobante);
			if (totalValue == null)
				return ResultadoControlMontosGlobales.Fallo("El monto total es nulo.");

			decimal total;
			try
			{
				total = Convert.ToDecimal(totalValue);
			}
			catch
			{
				return ResultadoControlMontosGlobales.Fallo("No se pudo convertir el monto total a decimal.");
			}

			// Regla: el monto total no puede ser negativo
			if (total < 0)
				return ResultadoControlMontosGlobales.Fallo("El monto total no puede ser negativo.");

			// Regla: el monto total no puede exceder el máximo permitido por la SUNAT (ejemplo: 1 millón)
			const decimal MONTO_MAXIMO = 1000000m;
			if (total > MONTO_MAXIMO)
				return ResultadoControlMontosGlobales.Fallo($"El monto total excede el máximo permitido ({MONTO_MAXIMO}).");

			// Regla: si el comprobante es de tipo Boleta, el monto no puede exceder 700 soles
			if (tipoProp != null)
			{
				var tipoValue = tipoProp.GetValue(comprobante);
				if (tipoValue != null)
				{
					var esBoletaProp = tipoValue.GetType().GetProperty("EsBoleta");
					if (esBoletaProp != null)
					{
						var esBoletaObj = esBoletaProp.GetValue(tipoValue);
						bool esBoleta = false;
						if (esBoletaObj is bool b)
						{
							esBoleta = b;
						}
						else if (esBoletaObj != null)
						{
							// Intentar convertir si es otro tipo
							try
							{
								esBoleta = Convert.ToBoolean(esBoletaObj);
							}
							catch
							{
								// Si no se puede convertir, no es boleta
								esBoleta = false;
							}
						}
						if (esBoleta && total > 700m)
							return ResultadoControlMontosGlobales.Fallo("El monto total de una Boleta no puede exceder 700 soles.");
					}
				}
			}

			return ResultadoControlMontosGlobales.Exito();
		}

		/// <summary>
		/// Resultado detallado de la validación de montos globales.
		/// </summary>
		public class ResultadoControlMontosGlobales
		{
			public bool EsValido { get; }
			public string Motivo { get; }

			private ResultadoControlMontosGlobales(bool esValido, string motivo)
			{
				EsValido = esValido;
				Motivo = motivo;
			}

			public static ResultadoControlMontosGlobales Exito()
				=> new ResultadoControlMontosGlobales(true, "Monto global válido.");

			public static ResultadoControlMontosGlobales Fallo(string motivo)
				=> new ResultadoControlMontosGlobales(false, motivo);
		}
	}
		// Usar el agregado real en vez de stub duplicado
}

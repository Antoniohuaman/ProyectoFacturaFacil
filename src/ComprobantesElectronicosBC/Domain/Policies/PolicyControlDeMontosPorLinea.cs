using System;
using System.Collections.Generic;

namespace ComprobantesElectronicosBC.Domain.Policies
{
	/// <summary>
	/// Política para el control normativo de los montos por línea en comprobantes electrónicos.
	/// </summary>
	public class PolicyControlDeMontosPorLinea
	{
		/// <summary>
		/// Valida que el monto de cada línea del comprobante cumpla con las reglas normativas.
		/// </summary>
		/// <param name="detalles">Lista de detalles del comprobante.</param>
		/// <returns>True si todos los montos son válidos, false si alguna línea no cumple las reglas.</returns>
		public ResultadoValidacionMontosPorLinea ValidarMontosPorLinea(List<DetalleComprobante> detalles)
		{
			if (detalles == null)
			{
				return new ResultadoValidacionMontosPorLinea
				{
					EsExitoso = false,
					MotivoFallo = "La lista de detalles es nula.",
					IndiceLineaFallo = null
				};
			}

			for (int i = 0; i < detalles.Count; i++)
			{
				var linea = detalles[i];
				if (linea == null)
				{
					return new ResultadoValidacionMontosPorLinea
					{
						EsExitoso = false,
						MotivoFallo = $"La línea {i} es nula.",
						IndiceLineaFallo = i
					};
				}

				if (linea.Monto < 0)
				{
					return new ResultadoValidacionMontosPorLinea
					{
						EsExitoso = false,
						MotivoFallo = $"El monto de la línea {i} es negativo.",
						IndiceLineaFallo = i
					};
				}

				if ((linea.TipoProducto ?? string.Empty).Equals("Servicio", StringComparison.OrdinalIgnoreCase) && linea.Monto < 1m)
				{
					return new ResultadoValidacionMontosPorLinea
					{
						EsExitoso = false,
						MotivoFallo = $"El monto de la línea {i} para un servicio es menor a 1 sol.",
						IndiceLineaFallo = i
					};
				}

				if (linea.Monto > 100000m)
				{
					return new ResultadoValidacionMontosPorLinea
					{
						EsExitoso = false,
						MotivoFallo = $"El monto de la línea {i} excede el máximo permitido.",
						IndiceLineaFallo = i
					};
				}
			}

			return new ResultadoValidacionMontosPorLinea
			{
				EsExitoso = true,
				MotivoFallo = null,
				IndiceLineaFallo = null
			};
		}
	}

	// Stub para ejemplo, reemplazar por el agregado real
	public class DetalleComprobante
	{
		public decimal Monto { get; set; }
	public string? TipoProducto { get; set; }
		// ...otros miembros...
	}

	public class ResultadoValidacionMontosPorLinea
	{
		public bool EsExitoso { get; set; }
	public string? MotivoFallo { get; set; }
		public int? IndiceLineaFallo { get; set; }
	}
}

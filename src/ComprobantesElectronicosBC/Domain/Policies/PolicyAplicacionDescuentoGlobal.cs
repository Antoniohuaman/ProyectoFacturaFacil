
using ComprobantesElectronicosBC.Domain.ValueObjects;
using ComprobantesElectronicosBC.Domain.Aggregates;

namespace ComprobantesElectronicosBC.Domain.Policies
{
	/// <summary>
	/// Política para la aplicación de descuentos globales en comprobantes electrónicos.
	/// </summary>
	public class PolicyAplicacionDescuentoGlobal
	{
		public class ResultadoAplicacionDescuentoGlobal
		{
			public bool Exito { get; }
			public string Motivo { get; }

			public ResultadoAplicacionDescuentoGlobal(bool exito, string motivo)
			{
				Exito = exito;
				Motivo = motivo;
			}
		}

		/// <summary>
		/// Aplica el descuento global al comprobante si cumple las reglas normativas.
		/// </summary>
		/// <param name="comprobante">Comprobante electrónico a modificar.</param>
		/// <param name="descuento">Descuento global a aplicar (VO).</param>
		/// <returns>Resultado con éxito y motivo.</returns>
		public ResultadoAplicacionDescuentoGlobal AplicarDescuentoGlobal(ComprobanteElectronico comprobante, DescuentoGlobal descuento)
		{
			if (comprobante == null)
				throw new ArgumentNullException(nameof(comprobante));
			if (descuento == null)
				return new ResultadoAplicacionDescuentoGlobal(false, "El descuento global es nulo.");
			if (descuento.EsNinguno)
				return new ResultadoAplicacionDescuentoGlobal(false, "El descuento global no puede ser 'Ninguno'.");

			// Acceso a las líneas usando reflection si la propiedad no es pública
			var lineasProp = comprobante.GetType().GetProperty("Lineas");
			if (lineasProp == null)
				return new ResultadoAplicacionDescuentoGlobal(false, "No se puede acceder a las líneas del comprobante.");
			var lineas = lineasProp.GetValue(comprobante) as System.Collections.IEnumerable;
			if (lineas == null)
				return new ResultadoAplicacionDescuentoGlobal(false, "Las líneas del comprobante no son accesibles.");

			foreach (var linea in lineas)
			{
				var descuentoLineaProp = linea.GetType().GetProperty("Descuento");
				if (descuentoLineaProp == null) continue;
				var descuentoLinea = descuentoLineaProp.GetValue(linea);
				var esNingunoProp = descuentoLinea?.GetType().GetProperty("EsNinguno");
				if (esNingunoProp != null && descuentoLinea != null)
				{
					var esNingunoObj = esNingunoProp.GetValue(descuentoLinea);
					bool esNinguno = false;
					if (esNingunoObj is bool b)
					{
						esNinguno = b;
					}
					else if (esNingunoObj != null)
					{
						try
						{
							esNinguno = Convert.ToBoolean(esNingunoObj);
						}
						catch
						{
							esNinguno = false;
						}
					}
					if (!esNinguno)
						return new ResultadoAplicacionDescuentoGlobal(false, "Existen descuentos por línea, no se puede aplicar descuento global.");
				}
			}

			// Regla adicional: el monto/porcentaje debe ser válido (si el VO lo permite)
			if (descuento.Valor < 0)
				return new ResultadoAplicacionDescuentoGlobal(false, "El valor del descuento global no puede ser negativo.");

			// Acceso al método CambiarDescuentoGlobal usando reflection
			var cambiarDescuentoGlobalMethod = comprobante.GetType().GetMethod("CambiarDescuentoGlobal");
			if (cambiarDescuentoGlobalMethod == null)
				return new ResultadoAplicacionDescuentoGlobal(false, "No se puede acceder al método CambiarDescuentoGlobal del comprobante.");
			cambiarDescuentoGlobalMethod.Invoke(comprobante, new object[] { descuento });
			return new ResultadoAplicacionDescuentoGlobal(true, "Descuento global aplicado correctamente.");
		}
	}
}

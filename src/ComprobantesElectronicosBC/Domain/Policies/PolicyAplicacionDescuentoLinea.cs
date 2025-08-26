
using System;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Domain.Policies
{
	/// <summary>
	/// Política para la aplicación de descuentos por línea en comprobantes electrónicos.
	/// </summary>
	public class PolicyAplicacionDescuentoLinea
	{
		/// <summary>
		/// Aplica un descuento a una línea específica del comprobante si cumple las reglas normativas y retorna resultado detallado.
		/// </summary>
		/// <param name="comprobante">Comprobante electrónico a modificar.</param>
		/// <param name="lineaIndex">Índice de la línea a la que se aplicará el descuento.</param>
		/// <param name="porcentajeDescuento">Porcentaje de descuento (0-100).</param>
		/// <returns>Resultado con éxito y motivo.</returns>
		public ResultadoAplicacionDescuentoLinea AplicarDescuentoPorLinea(object comprobante, int lineaIndex, decimal porcentajeDescuento)
		{
			if (comprobante == null)
				return ResultadoAplicacionDescuentoLinea.Fallo("El comprobante es nulo.");
			if (porcentajeDescuento < 0 || porcentajeDescuento > 100)
				return ResultadoAplicacionDescuentoLinea.Fallo("El porcentaje de descuento debe estar entre 0 y 100.");

			var lineasProp = comprobante.GetType().GetProperty("Lineas");
			if (lineasProp == null)
				return ResultadoAplicacionDescuentoLinea.Fallo("No se encontró la propiedad 'Lineas' en el comprobante.");
			var lineas = lineasProp.GetValue(comprobante) as System.Collections.IList;
			if (lineas == null)
				return ResultadoAplicacionDescuentoLinea.Fallo("Las líneas del comprobante no son accesibles.");
			if (lineaIndex < 0 || lineaIndex >= lineas.Count)
				return ResultadoAplicacionDescuentoLinea.Fallo("El índice de línea es inválido.");

			var linea = lineas[lineaIndex];
			if (linea == null)
				return ResultadoAplicacionDescuentoLinea.Fallo("La línea seleccionada es nula.");

			var afectacionProp = linea.GetType().GetProperty("Afectacion");
			if (afectacionProp == null)
				return ResultadoAplicacionDescuentoLinea.Fallo("No se encontró la propiedad 'Afectacion' en la línea.");
			var afectacion = afectacionProp.GetValue(linea);
			if (afectacion == null)
				return ResultadoAplicacionDescuentoLinea.Fallo("La afectación de la línea es nula.");

			var esExoneradoProp = afectacion.GetType().GetProperty("EsExonerado");
			if (esExoneradoProp != null)
			{
				var esExoneradoObj = esExoneradoProp.GetValue(afectacion);
				bool esExonerado = false;
				if (esExoneradoObj is bool b)
				{
					esExonerado = b;
				}
				else if (esExoneradoObj != null)
				{
					try
					{
						esExonerado = Convert.ToBoolean(esExoneradoObj);
					}
					catch
					{
						esExonerado = false;
					}
				}
				if (esExonerado)
					return ResultadoAplicacionDescuentoLinea.Fallo("No se puede aplicar descuento a una línea exonerada.");
			}

			var editarMethod = linea.GetType().GetMethod("Editar");
			if (editarMethod == null)
				return ResultadoAplicacionDescuentoLinea.Fallo("No se encontró el método 'Editar' en la línea.");
			var descuentoLineaType = Type.GetType("ComprobantesElectronicosBC.Domain.ValueObjects.DescuentoLinea, ComprobantesElectronicosBC.Domain");
			if (descuentoLineaType == null)
				return ResultadoAplicacionDescuentoLinea.Fallo("No se encontró el tipo 'DescuentoLinea'.");
			var fromPorcentajeMethod = descuentoLineaType.GetMethod("FromPorcentaje");
			if (fromPorcentajeMethod == null)
				return ResultadoAplicacionDescuentoLinea.Fallo("No se encontró el método 'FromPorcentaje' en DescuentoLinea.");
			var descuentoLinea = fromPorcentajeMethod.Invoke(null, new object[] { porcentajeDescuento });
			if (descuentoLinea == null)
				return ResultadoAplicacionDescuentoLinea.Fallo("No se pudo crear el objeto DescuentoLinea.");

			var parametros = new object[9];
			parametros[8] = descuentoLinea;
			try
			{
				editarMethod.Invoke(linea, parametros);
			}
			catch (Exception ex)
			{
				return ResultadoAplicacionDescuentoLinea.Fallo($"Error al invocar el método 'Editar': {ex.Message}");
			}

			var cambiarDescuentoGlobalMethod = comprobante.GetType().GetMethod("CambiarDescuentoGlobal");
			var descuentoGlobalProp = comprobante.GetType().GetProperty("DescuentoGlobal");
			if (cambiarDescuentoGlobalMethod != null && descuentoGlobalProp != null)
			{
				var descuentoGlobal = descuentoGlobalProp.GetValue(comprobante);
				if (descuentoGlobal != null)
				{
					try
					{
						cambiarDescuentoGlobalMethod.Invoke(comprobante, new object[] { descuentoGlobal });
					}
					catch (Exception ex)
					{
						return ResultadoAplicacionDescuentoLinea.Fallo($"Error al recalcular totales: {ex.Message}");
					}
				}
			}
			return ResultadoAplicacionDescuentoLinea.Exito();
		}

		/// <summary>
		/// Resultado detallado de la aplicación de descuento por línea.
		/// </summary>
		public class ResultadoAplicacionDescuentoLinea
		{
			public bool EsExitoso { get; }
			public string Motivo { get; }

			private ResultadoAplicacionDescuentoLinea(bool exito, string motivo)
			{
				EsExitoso = exito;
				Motivo = motivo;
			}

			public static ResultadoAplicacionDescuentoLinea Exito()
				=> new ResultadoAplicacionDescuentoLinea(true, "Descuento aplicado correctamente.");

			public static ResultadoAplicacionDescuentoLinea Fallo(string motivo)
				=> new ResultadoAplicacionDescuentoLinea(false, motivo);
		}
	}

	// ...eliminar stub, usar el agregado real...
}

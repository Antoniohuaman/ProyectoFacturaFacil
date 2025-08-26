using System;

namespace ComprobantesElectronicosBC.Domain.Policies
{
	/// <summary>
	/// Política para el control normativo de emisión retroactiva de comprobantes electrónicos.
	/// </summary>
	public class PolicyEmisionRetroactiva
	{
		/// <summary>
		/// Valida si la fecha de emisión retroactiva es permitida según las reglas normativas.
		/// </summary>
		/// <param name="fechaEmision">Fecha de emisión solicitada.</param>
		/// <param name="fechaActual">Fecha actual del sistema.</param>
		/// <returns>Resultado detallado de la validación.</returns>
		public ResultadoValidacionEmisionRetroactiva ValidarEmisionRetroactiva(DateTime fechaEmision, DateTime fechaActual)
		{
			// Regla: no se permite emitir comprobantes con fecha futura
			if (fechaEmision > fechaActual)
			{
				return new ResultadoValidacionEmisionRetroactiva
				{
					EsExitoso = false,
					MotivoFallo = "No se permite emitir comprobantes con fecha futura."
				};
			}

			// Regla: solo se permite emitir retroactivamente hasta 7 días atrás
			if ((fechaActual - fechaEmision).TotalDays > 7)
			{
				return new ResultadoValidacionEmisionRetroactiva
				{
					EsExitoso = false,
					MotivoFallo = "La emisión retroactiva solo se permite hasta 7 días atrás."
				};
			}

			return new ResultadoValidacionEmisionRetroactiva
			{
				EsExitoso = true,
				MotivoFallo = null
			};
		}
	}

	public class ResultadoValidacionEmisionRetroactiva
	{
		public bool EsExitoso { get; set; }
		public string? MotivoFallo { get; set; }
	}
}

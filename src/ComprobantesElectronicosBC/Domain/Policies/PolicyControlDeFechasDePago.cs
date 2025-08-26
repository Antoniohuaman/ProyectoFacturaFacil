using System;
using System.Collections.Generic;

namespace ComprobantesElectronicosBC.Domain.Policies
{
	/// <summary>
	/// Política para el control normativo de las fechas de pago en comprobantes electrónicos.
	/// </summary>
	public class PolicyControlDeFechasDePago
	{
		/// <summary>
		/// Valida y registra una fecha de pago en el comprobante según las reglas normativas.
		/// </summary>
		/// <param name="comprobante">Comprobante electrónico a modificar.</param>
		/// <param name="fechaPago">Fecha de pago a registrar.</param>
		/// <returns>True si la fecha es válida y se registra, false si no cumple las reglas.</returns>
		public bool RegistrarFechaDePago(ComprobanteElectronico comprobante, DateTime fechaPago)
		{
			if (comprobante == null)
				throw new ArgumentNullException(nameof(comprobante));

			// Regla: la fecha de pago no puede ser anterior a la fecha de emisión
			if (fechaPago < comprobante.FechaEmision)
				return false;

			// Regla: la fecha de pago no puede ser mayor a 1 año desde la emisión
			if (fechaPago > comprobante.FechaEmision.AddYears(1))
				return false;

			comprobante.FechaDePago = fechaPago;
			return true;
		}
	}

	// Stub para ejemplo, reemplazar por el agregado real
	public class ComprobanteElectronico
	{
		public DateTime FechaEmision { get; set; }
		public DateTime? FechaDePago { get; set; }
		// ...otros miembros...
	}
}

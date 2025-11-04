using System;
using SharedKernel.Exceptions;

namespace GestionInventarioBC.Domain.ValueObjects
{
	/// <summary>
	/// Periodo de inventario con fecha de inicio y fin (inclusive).
	/// </summary>
	public sealed record PeriodoInventario
	{
		public DateOnly Desde { get; }
		public DateOnly Hasta { get; }

		private PeriodoInventario(DateOnly desde, DateOnly hasta)
		{
			if (hasta < desde)
				throw new BusinessRuleException("El fin del período no puede ser anterior al inicio.");
			Desde = desde;
			Hasta = hasta;
		}

		public static PeriodoInventario Crear(DateOnly desde, DateOnly hasta) => new(desde, hasta);

		/// <summary>Crea un periodo mensual (primer día a último día del mes).</summary>
		public static PeriodoInventario Mensual(int anio, int mes)
		{
			var inicio = new DateOnly(anio, mes, 1);
			var fin = inicio.AddMonths(1).AddDays(-1);
			return new PeriodoInventario(inicio, fin);
		}

		public bool Contiene(DateOnly fecha) => fecha >= Desde && fecha <= Hasta;
	}
}


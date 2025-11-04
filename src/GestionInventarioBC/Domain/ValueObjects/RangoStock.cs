using SharedKernel.Exceptions;

namespace GestionInventarioBC.Domain.ValueObjects
{
	/// <summary>
	/// Rango operativo de stock (mínimo recomendado y máximo permitido).
	/// Nota: la pertenencia al rango considera (0, Max] según tests;
	/// el mínimo se usa típicamente para alertas, no para exclusión.
	/// </summary>
	public sealed record RangoStock
	{
		public StockMinimo Minimo { get; }
		public StockMaximo Maximo { get; }

		private RangoStock(StockMinimo min, StockMaximo max)
		{
			Minimo = min;
			Maximo = max;
		}

		public static RangoStock Crear(StockMinimo min, StockMaximo max)
		{
			if (max.Value < min.Value)
				throw new BusinessRuleException("El stock máximo no puede ser menor que el mínimo.");
			return new RangoStock(min, max);
		}

		/// <summary>
		/// Determina si una cantidad está dentro del rango operativo: (0, Max].
		/// </summary>
		public bool DentroDelRango(decimal cantidad) => cantidad > 0m && cantidad <= Maximo.Value;
	}
}


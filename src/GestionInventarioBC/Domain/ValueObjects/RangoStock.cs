using SharedKernel.Exceptions;

namespace GestionInventarioBC.Domain.ValueObjects
{
	/// <summary>
	/// Rango de stock (mínimo y máximo). Garantiza que 0 <= Min <= Max.
	/// </summary>
	public sealed record RangoStock
	{
		public StockMinimo Minimo { get; }
		public StockMaximo Maximo { get; }

		private RangoStock(StockMinimo minimo, StockMaximo maximo)
		{
			if (minimo.Value > maximo.Value)
				throw new BusinessRuleException("El stock mínimo no puede ser mayor que el máximo.");
			Minimo = minimo;
			Maximo = maximo;
		}

		public static RangoStock Crear(StockMinimo minimo, StockMaximo maximo)
			=> new(minimo, maximo);

		public bool DentroDelRango(decimal cantidad)
			=> cantidad >= Minimo.Value && cantidad <= Maximo.Value;
	}
}


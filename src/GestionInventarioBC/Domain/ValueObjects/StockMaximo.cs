using SharedKernel.Exceptions;

namespace GestionInventarioBC.Domain.ValueObjects
{
	/// <summary>
	/// Umbral máximo de stock (>= 0).
	/// </summary>
	public readonly record struct StockMaximo
	{
		public decimal Value { get; }

		public StockMaximo(decimal value)
		{
			if (value < 0)
				throw new BusinessRuleException("Stock máximo no puede ser negativo.");
			Value = decimal.Round(value, 6, System.MidpointRounding.AwayFromZero);
		}

		public static StockMaximo From(decimal value) => new(value);
		public override string ToString() => Value.ToString("0.######");
	}
}


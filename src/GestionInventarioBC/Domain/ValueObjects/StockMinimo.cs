using SharedKernel.Exceptions;

namespace GestionInventarioBC.Domain.ValueObjects
{
	/// <summary>
	/// Umbral mínimo de stock (no negativo).
	/// </summary>
	public readonly record struct StockMinimo
	{
		public decimal Value { get; }

		public StockMinimo(decimal value)
		{
			if (value < 0)
				throw new BusinessRuleException("Stock mínimo no puede ser negativo.");
			Value = decimal.Round(value, 6, System.MidpointRounding.AwayFromZero);
		}

		public static StockMinimo From(decimal value) => new(value);
		public override string ToString() => Value.ToString("0.######");
	}
}


using System;
using System.Globalization;
using SharedKernel.Exceptions;

namespace GestionInventarioBC.Domain.ValueObjects
{
	/// <summary>
	/// Stock mínimo recomendado (no negativo), redondeado a 6 decimales.
	/// </summary>
	public sealed record StockMinimo
	{
		public decimal Value { get; }

		public StockMinimo(decimal value)
		{
			var rounded = Math.Round(value, 6, MidpointRounding.AwayFromZero);
			if (rounded < 0m)
				throw new BusinessRuleException("El stock mínimo no puede ser negativo.");
			Value = rounded;
		}

		public override string ToString() => Value.ToString("0.######", CultureInfo.InvariantCulture);

		public static StockMinimo From(decimal value)
		{
			var rounded = Math.Round(value, 6, MidpointRounding.AwayFromZero);
			if (rounded < 0m)
				throw new BusinessRuleException("Stock mínimo no puede ser negativo");
			return new StockMinimo(rounded);
		}
	}
}


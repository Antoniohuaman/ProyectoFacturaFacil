using System;
using System.Globalization;
using SharedKernel.Exceptions;

namespace GestionInventarioBC.Domain.ValueObjects
{
	/// <summary>
	/// Stock máximo permitido (no negativo), redondeado a 6 decimales.
	/// </summary>
	public sealed record StockMaximo
	{
		public decimal Value { get; }

		public StockMaximo(decimal value)
		{
			var rounded = Math.Round(value, 6, MidpointRounding.AwayFromZero);
			if (rounded < 0m)
				throw new BusinessRuleException("El stock máximo no puede ser negativo.");
			Value = rounded;
		}

		public override string ToString() => Value.ToString("0.######", CultureInfo.InvariantCulture);
	}
}


using System;
using System.Globalization;
using SharedKernel.Exceptions;

namespace GestionInventarioBC.Domain.ValueObjects
{
	/// <summary>
	/// Cantidad de stock con hasta 6 decimales. No permite valores negativos.
	/// Incluye operadores + y - que respetan la no-negatividad.
	/// </summary>
	public sealed record CantidadStock
	{
		public decimal Value { get; }

		public CantidadStock(decimal value)
		{
			var rounded = Math.Round(value, 6, MidpointRounding.AwayFromZero);
			if (rounded < 0m)
				throw new BusinessRuleException("La cantidad no puede ser negativa.");
			Value = rounded;
		}

		public static CantidadStock From(decimal value) => new(value);
		public static CantidadStock Cero => new(0m);

		public override string ToString() => Value.ToString("0.######", CultureInfo.InvariantCulture);

		public static CantidadStock operator +(CantidadStock a, CantidadStock b)
			=> From(a.Value + b.Value);

		public static CantidadStock operator -(CantidadStock a, CantidadStock b)
		{
			var result = Math.Round(a.Value - b.Value, 6, MidpointRounding.AwayFromZero);
			if (result < 0m)
				throw new BusinessRuleException("La cantidad resultante no puede ser negativa.");
			return From(result);
		}
	}
}


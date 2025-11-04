using GestionInventarioBC.Domain.ValueObjects;
using SharedKernel.Specifications;

namespace GestionInventarioBC.Domain.Specifications
{
	/// <summary>
	/// El rango es válido cuando Min <= Max.
	/// </summary>
	public sealed class RangoStockValidoSpec : IBooleanSpecification<RangoStock>
	{
		public bool IsSatisfiedBy(RangoStock r) => r.Minimo.Value <= r.Maximo.Value;
	}
}


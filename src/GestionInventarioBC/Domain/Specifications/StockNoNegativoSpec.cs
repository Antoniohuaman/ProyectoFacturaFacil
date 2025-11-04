using GestionInventarioBC.Domain.Aggregates;
using SharedKernel.Specifications;

namespace GestionInventarioBC.Domain.Specifications
{
	/// <summary>
	/// Verifica que el stock no sea negativo y que Reservado <= Real.
	/// </summary>
	public sealed class StockNoNegativoSpec : IBooleanSpecification<StockPorAlmacen>
	{
		public bool IsSatisfiedBy(StockPorAlmacen s)
			=> s.Real.Value >= 0m && s.Reservado.Value >= 0m && s.Reservado.Value <= s.Real.Value;
	}
}


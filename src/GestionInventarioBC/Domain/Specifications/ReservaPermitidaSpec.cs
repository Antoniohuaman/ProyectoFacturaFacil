using GestionInventarioBC.Domain.Aggregates;
using GestionInventarioBC.Domain.ValueObjects;
using SharedKernel.Specifications;

namespace GestionInventarioBC.Domain.Specifications
{
	/// <summary>
	/// La reserva es permitida si la cantidad solicitada no excede el disponible.
	/// </summary>
	public sealed class ReservaPermitidaSpec : IBooleanSpecification<(StockPorAlmacen Stock, CantidadStock Cantidad)>
	{
		public bool IsSatisfiedBy((StockPorAlmacen Stock, CantidadStock Cantidad) x)
			=> x.Cantidad.Value <= x.Stock.Disponible.Value;
	}
}


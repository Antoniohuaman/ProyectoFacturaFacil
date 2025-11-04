using GestionInventarioBC.Domain.Aggregates;
using SharedKernel.Specifications;

namespace GestionInventarioBC.Domain.Specifications
{
	/// <summary>
	/// Transferencia válida si origen y destino son distintos y cantidad > 0.
	/// </summary>
	public sealed class TransferenciaValidaSpec : IBooleanSpecification<TransferenciaInventario>
	{
		public bool IsSatisfiedBy(TransferenciaInventario t)
			=> (t.OrigenEstablecimientoId != t.DestinoEstablecimientoId || t.OrigenAlmacenId != t.DestinoAlmacenId)
			   && t.Cantidad.Value > 0m;
	}
}


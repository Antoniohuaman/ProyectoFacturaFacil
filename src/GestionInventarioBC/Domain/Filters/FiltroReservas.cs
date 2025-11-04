using System.Collections.Generic;
using GestionInventarioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Domain.Filters
{
	public sealed record FiltroReservas(
		EmpresaId EmpresaId,
		EstablecimientoId? EstablecimientoId,
		AlmacenId? AlmacenId,
		IReadOnlyCollection<EstadoReserva>? Estados,
		string? Sku
	);
}


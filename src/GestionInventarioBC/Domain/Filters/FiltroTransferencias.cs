using System.Collections.Generic;
using GestionInventarioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Domain.Filters
{
	public sealed record FiltroTransferencias(
		EmpresaId EmpresaId,
		EstablecimientoId? OrigenEstablecimientoId,
		AlmacenId? OrigenAlmacenId,
		EstablecimientoId? DestinoEstablecimientoId,
		AlmacenId? DestinoAlmacenId,
		IReadOnlyCollection<EstadoTransferencia>? Estados,
		string? Sku
	);
}


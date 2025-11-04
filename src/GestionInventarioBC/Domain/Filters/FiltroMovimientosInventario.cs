using System;
using GestionInventarioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Domain.Filters
{
	public sealed record FiltroMovimientosInventario(
		EmpresaId EmpresaId,
		EstablecimientoId? EstablecimientoId,
		AlmacenId? AlmacenId,
		DateTimeOffset? Desde,
		DateTimeOffset? Hasta,
		TipoMovimiento? Tipo,
		MotivoMovimiento? Motivo,
		string? Sku
	);
}


using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects; // EmpresaId, EstablecimientoId, AlmacenId, Sku
using GestionInventarioBC.Domain.ValueObjects; // TipoMovimiento, MotivoMovimiento

namespace GestionInventarioBC.Domain.Events
{
	public sealed record MovimientoRegistrado(
		Guid MovimientoId,
		EmpresaId EmpresaId,
		EstablecimientoId EstablecimientoId,
		AlmacenId AlmacenId,
		DateTimeOffset Fecha,
		TipoMovimiento Tipo,
		MotivoMovimiento Motivo
	) : IDomainEvent;
}


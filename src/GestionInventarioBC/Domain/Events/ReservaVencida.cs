using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects; // EmpresaId, EstablecimientoId, AlmacenId, Sku

namespace GestionInventarioBC.Domain.Events
{
	public sealed record ReservaVencida(
		Guid ReservaId,
		EmpresaId EmpresaId,
		EstablecimientoId EstablecimientoId,
		AlmacenId AlmacenId,
		Sku Sku,
		decimal Cantidad
	) : IDomainEvent;
}


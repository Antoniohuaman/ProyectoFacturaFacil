using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects; // EmpresaId, EstablecimientoId, AlmacenId, ProductoId

namespace GestionInventarioBC.Domain.Events
{
	public sealed record ReservaCreada(
		Guid ReservaId,
		EmpresaId EmpresaId,
		EstablecimientoId EstablecimientoId,
		AlmacenId AlmacenId,
		ProductoId ProductoId,
		decimal Cantidad
	) : IDomainEvent;
}


using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects; // EmpresaId, EstablecimientoId, AlmacenId, ProductoId

namespace GestionInventarioBC.Domain.Events
{
	public sealed record TransferenciaConfirmada(
		Guid TransferenciaId,
		EmpresaId EmpresaId,
		EstablecimientoId OrigenEstablecimientoId,
		AlmacenId OrigenAlmacenId,
		EstablecimientoId DestinoEstablecimientoId,
		AlmacenId DestinoAlmacenId,
		ProductoId ProductoId,
		decimal Cantidad
	) : IDomainEvent;
}


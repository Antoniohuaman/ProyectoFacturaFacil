using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects; // EmpresaId, EstablecimientoId, AlmacenId, Sku

namespace GestionInventarioBC.Domain.Events
{
	public sealed record TransferenciaCancelada(
		Guid TransferenciaId,
		EmpresaId EmpresaId,
		EstablecimientoId OrigenEstablecimientoId,
		AlmacenId OrigenAlmacenId,
		EstablecimientoId DestinoEstablecimientoId,
		AlmacenId DestinoAlmacenId,
		Sku Sku,
		decimal Cantidad,
		string? Motivo
	) : IDomainEvent;
}


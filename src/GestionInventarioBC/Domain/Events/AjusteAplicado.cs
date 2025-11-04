using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects; // EmpresaId, EstablecimientoId, AlmacenId, Sku

namespace GestionInventarioBC.Domain.Events
{
	public sealed record AjusteAplicado(
		Guid MovimientoId,
		EmpresaId EmpresaId,
		EstablecimientoId EstablecimientoId,
		AlmacenId AlmacenId,
		Sku Sku,
		decimal Cantidad,
		bool EsPositivo
	) : IDomainEvent;
}


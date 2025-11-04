using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects; // EmpresaId, EstablecimientoId, AlmacenId, ProductoId

namespace GestionInventarioBC.Domain.Events
{
	public sealed record AjusteAplicado(
		Guid MovimientoId,
		EmpresaId EmpresaId,
		EstablecimientoId EstablecimientoId,
		AlmacenId AlmacenId,
		ProductoId ProductoId,
		decimal Cantidad,
		bool EsPositivo
	) : IDomainEvent;
}


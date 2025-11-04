using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects; // EmpresaId, EstablecimientoId, AlmacenId

namespace GestionInventarioBC.Domain.Events
{
	public sealed record MovimientoAnulado(
		Guid MovimientoId,
		EmpresaId EmpresaId,
		EstablecimientoId EstablecimientoId,
		AlmacenId AlmacenId,
		string? Motivo
	) : IDomainEvent;
}


using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace ComprobantesElectronicosBC.Domain.Events
{
	// Homogeneizado a sealed record inmutable que implementa IDomainEvent
	public sealed record ComprobanteCorregidoDomainEvent(
		EmpresaId EmpresaId,
		EstablecimientoId EstablecimientoId,
		Guid ComprobanteId,
		DateTime FechaCorreccion,
		string? MotivoCorreccion = null
	) : IDomainEvent;
}

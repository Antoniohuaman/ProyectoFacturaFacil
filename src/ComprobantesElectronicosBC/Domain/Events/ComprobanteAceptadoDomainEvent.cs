using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace ComprobantesElectronicosBC.Domain.Events
{
	// Homogeneizado a sealed record inmutable que implementa IDomainEvent
	public sealed record ComprobanteAceptadoDomainEvent(
		EmpresaId EmpresaId,
		EstablecimientoId EstablecimientoId,
		TenantId TenantId,
		Guid ComprobanteId,
		DateTime FechaAceptacion,
		string? Observaciones = null
	) : IDomainEvent;
}

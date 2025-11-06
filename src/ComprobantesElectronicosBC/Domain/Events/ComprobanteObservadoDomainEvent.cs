using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace ComprobantesElectronicosBC.Domain.Events
{
	/// <summary>
	/// Evento: El comprobante fue observado (error técnico o de validación).
	/// </summary>
	public sealed record ComprobanteObservadoDomainEvent(
		EmpresaId EmpresaId,
		EstablecimientoId EstablecimientoId,
		Guid ComprobanteId,
		string DetalleError,
		DateTime ObservadoEnUtc
	) : IDomainEvent;
}

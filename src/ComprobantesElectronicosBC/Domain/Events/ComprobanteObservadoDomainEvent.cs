using System;
using SharedKernel.Events;

namespace ComprobantesElectronicosBC.Domain.Events
{
	/// <summary>
	/// Evento: El comprobante fue observado (error técnico o de validación).
	/// </summary>
	public sealed record ComprobanteObservadoDomainEvent(
		Guid ComprobanteId,
		string DetalleError,
		DateTime ObservadoEnUtc
	) : IDomainEvent;
}

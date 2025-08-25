using System;
using SharedKernel.Events;

namespace ComprobantesElectronicosBC.Domain.Events
{
	/// <summary>
	/// Evento: El comprobante fue emitido (pasa a estado Enviado).
	/// </summary>
	public sealed record ComprobanteEmitidoDomainEvent(
		Guid ComprobanteId,
		DateTime EmitidoEnUtc
	) : IDomainEvent;
}

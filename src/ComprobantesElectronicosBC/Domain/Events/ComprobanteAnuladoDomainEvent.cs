using System;
using SharedKernel.Events;

namespace ComprobantesElectronicosBC.Domain.Events
{
	/// <summary>
	/// Evento: El comprobante fue anulado (baja confirmada).
	/// </summary>
	public sealed record ComprobanteAnuladoDomainEvent(
		Guid ComprobanteId,
		DateTime AnuladoEnUtc
	) : IDomainEvent;
}

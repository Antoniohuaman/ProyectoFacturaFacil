using System;
using SharedKernel.Events;

namespace ComprobantesElectronicosBC.Domain.Events
{
	/// <summary>
	/// Evento: El comprobante fue rechazado por SUNAT.
	/// </summary>
	public sealed record ComprobanteRechazadoDomainEvent(
		Guid ComprobanteId,
		string CodigoCdr,
		string Descripcion,
		DateTime RechazadoEnUtc
	) : IDomainEvent;
}

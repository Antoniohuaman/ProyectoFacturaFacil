using System;
using SharedKernel.Events;

namespace ComprobantesElectronicosBC.Domain.Events
{
	/// <summary>
	/// Evento: El comprobante fue enviado al API externo/SUNAT.
	/// </summary>
	public sealed record ComprobanteEnviadoDomainEvent(
		Guid ComprobanteId,
		DateTime EnviadoEnUtc
	) : IDomainEvent;
}

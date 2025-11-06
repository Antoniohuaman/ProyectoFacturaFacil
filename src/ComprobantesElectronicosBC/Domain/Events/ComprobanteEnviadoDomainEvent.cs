using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace ComprobantesElectronicosBC.Domain.Events
{
	/// <summary>
	/// Evento: El comprobante fue enviado al API externo/SUNAT.
	/// </summary>
	public sealed record ComprobanteEnviadoDomainEvent(
		EmpresaId EmpresaId,
		EstablecimientoId EstablecimientoId,
		Guid ComprobanteId,
		DateTime EnviadoEnUtc
	) : IDomainEvent;
}

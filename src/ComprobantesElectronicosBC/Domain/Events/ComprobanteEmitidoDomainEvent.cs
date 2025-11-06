using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace ComprobantesElectronicosBC.Domain.Events
{
	/// <summary>
	/// Evento: El comprobante fue emitido (pasa a estado Enviado).
	/// </summary>
	public sealed record ComprobanteEmitidoDomainEvent(
		EmpresaId EmpresaId,
		EstablecimientoId EstablecimientoId,
		Guid ComprobanteId,
		DateTime EmitidoEnUtc
	) : IDomainEvent;
}

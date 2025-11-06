using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace ComprobantesElectronicosBC.Domain.Events
{
	/// <summary>
	/// Evento: El comprobante fue anulado (baja confirmada).
	/// </summary>
	public sealed record ComprobanteAnuladoDomainEvent(
		EmpresaId EmpresaId,
		EstablecimientoId EstablecimientoId,
		Guid ComprobanteId,
		DateTime AnuladoEnUtc
	) : IDomainEvent;
}

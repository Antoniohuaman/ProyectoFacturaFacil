using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace ComprobantesElectronicosBC.Domain.Events
{
	/// <summary>
	/// Evento: El comprobante fue rechazado por SUNAT.
	/// </summary>
	public sealed record ComprobanteRechazadoDomainEvent(
		EmpresaId EmpresaId,
		EstablecimientoId EstablecimientoId,
		Guid ComprobanteId,
		string CodigoCdr,
		string Descripcion,
		DateTime RechazadoEnUtc
	) : IDomainEvent;
}

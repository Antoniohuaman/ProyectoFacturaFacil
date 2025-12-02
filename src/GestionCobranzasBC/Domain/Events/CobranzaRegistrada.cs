using System;
using System.Collections.Generic;
using GestionCobranzasBC.Domain.Entities;
using GestionCobranzasBC.Domain.ValueObjects;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace GestionCobranzasBC.Domain.Events;

/// <summary>
/// Evento emitido cuando se registra una cobranza sobre una cuenta por cobrar.
/// </summary>
public sealed class CobranzaRegistrada : DomainEvent
{
	public CobranzaRegistrada(
		CobranzaId cobranzaId,
		CuentaPorCobrarId cuentaPorCobrarId,
		string numeroCompleto,
		DateOnly fechaDocumento,
		Dinero montoTotal,
		CajaDestino cajaDestino,
		IReadOnlyCollection<LineaCobro> lineasCobro,
		Guid? eventId = null,
		DateTime? occurredOnUtc = null)
		: base(eventId, occurredOnUtc)
	{
		CobranzaId = cobranzaId;
		CuentaPorCobrarId = cuentaPorCobrarId;
		NumeroCompleto = numeroCompleto;
		FechaDocumento = fechaDocumento;
		MontoTotal = montoTotal;
		CajaDestino = cajaDestino;
		LineasCobro = lineasCobro;
	}

	public CobranzaId CobranzaId { get; }
	public CuentaPorCobrarId CuentaPorCobrarId { get; }
	public string NumeroCompleto { get; }
	public DateOnly FechaDocumento { get; }
	public Dinero MontoTotal { get; }
	public CajaDestino CajaDestino { get; }
	public IReadOnlyCollection<LineaCobro> LineasCobro { get; }
}

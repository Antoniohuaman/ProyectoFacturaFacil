using System;
using GestionCobranzasBC.Domain.Entities;
using GestionCobranzasBC.Domain.ValueObjects;
using SharedKernel.Events;

namespace GestionCobranzasBC.Domain.Events;

/// <summary>
/// Se dispara cuando una cuenta por cobrar pasa a estado vencido.
/// </summary>
public sealed class CuentaPorCobrarVencida : DomainEvent
{
	public CuentaPorCobrarVencida(
		CuentaPorCobrarId cuentaPorCobrarId,
		DocumentoOrigen documentoOrigen,
		Guid clienteId,
		SaldoPendiente saldo,
		DateOnly fechaVencimiento,
		Guid? eventId = null,
		DateTime? occurredOnUtc = null)
		: base(eventId, occurredOnUtc)
	{
		CuentaPorCobrarId = cuentaPorCobrarId;
		DocumentoOrigen = documentoOrigen;
		ClienteId = clienteId;
		Saldo = saldo;
		FechaVencimiento = fechaVencimiento;
	}

	public CuentaPorCobrarId CuentaPorCobrarId { get; }
	public DocumentoOrigen DocumentoOrigen { get; }
	public Guid ClienteId { get; }
	public SaldoPendiente Saldo { get; }
	public DateOnly FechaVencimiento { get; }
}

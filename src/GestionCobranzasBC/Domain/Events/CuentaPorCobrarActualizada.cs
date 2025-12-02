using System;
using GestionCobranzasBC.Domain.Entities;
using GestionCobranzasBC.Domain.ValueObjects;
using SharedKernel.Events;

namespace GestionCobranzasBC.Domain.Events;

/// <summary>
/// Se dispara cuando cambia el saldo o el estado de una cuenta por cobrar.
/// </summary>
public sealed class CuentaPorCobrarActualizada : DomainEvent
{
	public CuentaPorCobrarActualizada(
		CuentaPorCobrarId cuentaPorCobrarId,
		DocumentoOrigen documentoOrigen,
		Guid clienteId,
		SaldoPendiente saldo,
		EstadoCuentaPorCobrar estado,
		DateOnly fechaActualizacion,
		Guid? eventId = null,
		DateTime? occurredOnUtc = null)
		: base(eventId, occurredOnUtc)
	{
		CuentaPorCobrarId = cuentaPorCobrarId;
		DocumentoOrigen = documentoOrigen;
		ClienteId = clienteId;
		Saldo = saldo;
		Estado = estado;
		FechaActualizacion = fechaActualizacion;
	}

	public CuentaPorCobrarId CuentaPorCobrarId { get; }
	public DocumentoOrigen DocumentoOrigen { get; }
	public Guid ClienteId { get; }
	public SaldoPendiente Saldo { get; }
	public EstadoCuentaPorCobrar Estado { get; }
	public DateOnly FechaActualizacion { get; }
}

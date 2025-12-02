using System;
using GestionCobranzasBC.Domain.Entities;
using GestionCobranzasBC.Domain.ValueObjects;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace GestionCobranzasBC.Domain.Events;

/// <summary>
/// Se dispara cuando cambia el saldo o el estado de una cuenta por cobrar.
/// </summary>
public sealed class CuentaPorCobrarActualizada : DomainEvent
{
	public CuentaPorCobrarActualizada(
		EmpresaId empresaId,
		EstablecimientoId? establecimientoId,
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
		EmpresaId = empresaId ?? throw new ArgumentNullException(nameof(empresaId));
		EstablecimientoId = establecimientoId;
		CuentaPorCobrarId = cuentaPorCobrarId;
		DocumentoOrigen = documentoOrigen;
		ClienteId = clienteId;
		Saldo = saldo;
		Estado = estado;
		FechaActualizacion = fechaActualizacion;
	}

	public EmpresaId EmpresaId { get; }
	public EstablecimientoId? EstablecimientoId { get; }
	public CuentaPorCobrarId CuentaPorCobrarId { get; }
	public DocumentoOrigen DocumentoOrigen { get; }
	public Guid ClienteId { get; }
	public SaldoPendiente Saldo { get; }
	public EstadoCuentaPorCobrar Estado { get; }
	public DateOnly FechaActualizacion { get; }
}

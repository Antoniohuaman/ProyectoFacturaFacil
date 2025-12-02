using System;
using GestionCobranzasBC.Domain.Entities;
using GestionCobranzasBC.Domain.ValueObjects;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace GestionCobranzasBC.Domain.Events;

/// <summary>
/// Se dispara cuando una cuenta por cobrar pasa a estado vencido.
/// </summary>
public sealed class CuentaPorCobrarVencida : DomainEvent
{
	public CuentaPorCobrarVencida(
		EmpresaId empresaId,
		EstablecimientoId? establecimientoId,
		CuentaPorCobrarId cuentaPorCobrarId,
		DocumentoOrigen documentoOrigen,
		Guid clienteId,
		SaldoPendiente saldo,
		DateOnly fechaVencimiento,
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
		FechaVencimiento = fechaVencimiento;
	}

	public EmpresaId EmpresaId { get; }
	public EstablecimientoId? EstablecimientoId { get; }
	public CuentaPorCobrarId CuentaPorCobrarId { get; }
	public DocumentoOrigen DocumentoOrigen { get; }
	public Guid ClienteId { get; }
	public SaldoPendiente Saldo { get; }
	public DateOnly FechaVencimiento { get; }
}

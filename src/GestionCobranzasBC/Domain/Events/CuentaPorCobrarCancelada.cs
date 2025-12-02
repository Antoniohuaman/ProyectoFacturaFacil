using System;
using GestionCobranzasBC.Domain.Entities;
using GestionCobranzasBC.Domain.ValueObjects;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace GestionCobranzasBC.Domain.Events;

/// <summary>
/// Se dispara cuando la cuenta por cobrar queda completamente cancelada.
/// </summary>
public sealed class CuentaPorCobrarCancelada : DomainEvent
{
    public CuentaPorCobrarCancelada(
        EmpresaId empresaId,
        EstablecimientoId? establecimientoId,
        CuentaPorCobrarId cuentaPorCobrarId,
        DocumentoOrigen documentoOrigen,
        Guid clienteId,
        SaldoPendiente saldo,
        DateOnly fechaCancelacion,
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
        FechaCancelacion = fechaCancelacion;
    }

    public EmpresaId EmpresaId { get; }
    public EstablecimientoId? EstablecimientoId { get; }
    public CuentaPorCobrarId CuentaPorCobrarId { get; }
    public DocumentoOrigen DocumentoOrigen { get; }
    public Guid ClienteId { get; }
    public SaldoPendiente Saldo { get; }
    public DateOnly FechaCancelacion { get; }
}

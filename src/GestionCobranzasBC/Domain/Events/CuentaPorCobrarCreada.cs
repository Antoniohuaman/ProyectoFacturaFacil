using System;
using GestionCobranzasBC.Domain.Entities;
using GestionCobranzasBC.Domain.ValueObjects;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace GestionCobranzasBC.Domain.Events;

/// <summary>
/// Evento emitido cuando se crea una cuenta por cobrar.
/// </summary>
public sealed class CuentaPorCobrarCreada : DomainEvent
{
    public CuentaPorCobrarCreada(
        TenantId tenantId,
        EmpresaId empresaId,
        EstablecimientoId? establecimientoId,
        CuentaPorCobrarId cuentaPorCobrarId,
        DocumentoOrigen documentoOrigen,
        Guid clienteId,
        SaldoPendiente saldo,
        EstadoCuentaPorCobrar estado,
        DateOnly fechaRegistro,
        Guid? eventId = null,
        DateTime? occurredOnUtc = null)
        : base(eventId, occurredOnUtc)
    {
        TenantId = tenantId;
        EmpresaId = empresaId ?? throw new ArgumentNullException(nameof(empresaId));
        EstablecimientoId = establecimientoId;
        CuentaPorCobrarId = cuentaPorCobrarId;
        DocumentoOrigen = documentoOrigen;
        ClienteId = clienteId;
        Saldo = saldo;
        Estado = estado;
        FechaRegistro = fechaRegistro;
    }

    public TenantId TenantId { get; }
    public EmpresaId EmpresaId { get; }
    public EstablecimientoId? EstablecimientoId { get; }
    public CuentaPorCobrarId CuentaPorCobrarId { get; }
    public DocumentoOrigen DocumentoOrigen { get; }
    public Guid ClienteId { get; }
    public SaldoPendiente Saldo { get; }
    public EstadoCuentaPorCobrar Estado { get; }
    public DateOnly FechaRegistro { get; }
}

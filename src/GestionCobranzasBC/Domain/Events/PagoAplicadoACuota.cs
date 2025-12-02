using System;
using GestionCobranzasBC.Domain.Entities;
using GestionCobranzasBC.Domain.ValueObjects;
using SharedKernel.Events;

namespace GestionCobranzasBC.Domain.Events;

/// <summary>
/// Se dispara cuando un pago es aplicado a la cuenta por cobrar y afecta sus cuotas.
/// </summary>
public sealed class PagoAplicadoACuota : DomainEvent
{
    public PagoAplicadoACuota(
        CuentaPorCobrarId cuentaPorCobrarId,
        CobranzaId cobranzaId,
        DocumentoOrigen documentoOrigen,
        SaldoPendiente saldoDespuesDePago,
        EstadoCuentaPorCobrar estadoDespuesDePago,
        DateOnly fechaPago,
        Guid? eventId = null,
        DateTime? occurredOnUtc = null)
        : base(eventId, occurredOnUtc)
    {
        CuentaPorCobrarId = cuentaPorCobrarId;
        CobranzaId = cobranzaId;
        DocumentoOrigen = documentoOrigen;
        SaldoDespuesDePago = saldoDespuesDePago;
        EstadoDespuesDePago = estadoDespuesDePago;
        FechaPago = fechaPago;
    }

    public CuentaPorCobrarId CuentaPorCobrarId { get; }
    public CobranzaId CobranzaId { get; }
    public DocumentoOrigen DocumentoOrigen { get; }
    public SaldoPendiente SaldoDespuesDePago { get; }
    public EstadoCuentaPorCobrar EstadoDespuesDePago { get; }
    public DateOnly FechaPago { get; }
}

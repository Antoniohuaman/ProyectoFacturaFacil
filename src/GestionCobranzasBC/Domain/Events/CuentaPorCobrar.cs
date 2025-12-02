using System;
using GestionCobranzasBC.Domain.Entities;
using GestionCobranzasBC.Domain.ValueObjects;
using SharedKernel.Events;

namespace GestionCobranzasBC.Domain.Events;

/// <summary>
/// Evento genérico asociado a una cuenta por cobrar.
/// Útil para auditoría o logs cuando no se requiere un tipo específico.
/// </summary>
public sealed class CuentaPorCobrarGenerica : DomainEvent
{
    public CuentaPorCobrarGenerica(
        CuentaPorCobrarId cuentaPorCobrarId,
        DocumentoOrigen documentoOrigen,
        string descripcion,
        Guid? eventId = null,
        DateTime? occurredOnUtc = null)
        : base(eventId, occurredOnUtc)
    {
        CuentaPorCobrarId = cuentaPorCobrarId;
        DocumentoOrigen = documentoOrigen;
        Descripcion = descripcion;
    }

    public CuentaPorCobrarId CuentaPorCobrarId { get; }
    public DocumentoOrigen DocumentoOrigen { get; }
    public string Descripcion { get; }
}

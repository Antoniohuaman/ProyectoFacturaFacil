using System;
using SharedKernel.Events;

namespace GestionCobranzasBC.Domain.Events;

/// <summary>
/// Se dispara cuando se crea una cuenta por cobrar a partir de un comprobante.
/// </summary>
public sealed record CuentaPorCobrarCreada(
    string CuentaPorCobrarId,
    string ComprobanteId,
    string TipoDocumento, // 01, 03, etc.
    string Serie,
    string Numero,
    DateOnly FechaEmision,
    string MonedaCodigo,
    decimal ImporteTotal
) : DomainEvent;

using System;
using SharedKernel.Events;

namespace GestionCobranzasBC.Domain.Events;

/// <summary>
/// Se dispara cuando se aplica un pago a una cuota específica del cronograma.
/// </summary>
public sealed record PagoAplicadoACuota(
    string CuentaPorCobrarId,
    int NumeroCuota,
    decimal MontoAplicado,
    DateTimeOffset FechaPago
) : DomainEvent;

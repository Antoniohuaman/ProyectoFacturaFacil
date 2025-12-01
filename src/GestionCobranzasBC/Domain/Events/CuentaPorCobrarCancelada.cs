using System;
using SharedKernel.Events;

namespace GestionCobranzasBC.Domain.Events;

/// <summary>
/// Se dispara cuando la cuenta por cobrar queda completamente cancelada.
/// </summary>
    public record CuentaPorCobrarCancelada : DomainEvent
    {
        public CuentaPorCobrarId CuentaPorCobrarId { get; init; }

        public CuentaPorCobrarCancelada(CuentaPorCobrarId cuentaPorCobrarId)
        {
            CuentaPorCobrarId = cuentaPorCobrarId;
        }
    }

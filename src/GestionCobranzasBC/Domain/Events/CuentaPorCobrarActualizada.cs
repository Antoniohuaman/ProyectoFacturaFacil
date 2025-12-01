using SharedKernel.Events;

namespace GestionCobranzasBC.Domain.Events;

/// <summary>
/// Se dispara cuando cambia el saldo o el estado de una cuenta por cobrar.
/// </summary>
public sealed record CuentaPorCobrarActualizada(CuentaPorCobrarId CuentaPorCobrarId) : DomainEvent;

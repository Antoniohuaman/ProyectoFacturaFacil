using System;
using SharedKernel.Events;

namespace GestionCobranzasBC.Domain.Events;

/// <summary>
/// Se dispara cuando una cuenta por cobrar pasa a estado vencido.
/// </summary>
using GestionCobranzasBC.Domain.ValueObjects;

public sealed record CuentaPorCobrarVencida(CuentaPorCobrarId CuentaPorCobrarId) : DomainEvent;

using SharedKernel.Events;

namespace GestionCobranzasBC.Domain.Events;

/// <summary>
/// Evento genérico asociado a una cuenta por cobrar.
/// Útil para auditoría o logs cuando no se requiere un tipo específico.
/// </summary>
public sealed record CuentaPorCobrar(
    string CuentaPorCobrarId,
    string Descripcion
) : DomainEvent;

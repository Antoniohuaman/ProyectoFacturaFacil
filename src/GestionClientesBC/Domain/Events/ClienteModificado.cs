using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Domain.Events
{
    /// <summary>
    /// Evento de dominio que indica que un cliente ha sido modificado.
    /// </summary>
    public sealed record ClienteModificado(
        Guid ClienteId,
        IDictionary<string, (object? anterior, object? nuevo)> Cambios,
        DateTime FechaModificacion
    ) : IDomainEvent;
}

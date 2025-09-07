using System;
using SharedKernel.Events;

namespace GestionClientesBC.Domain.Events
{
    /// <summary>
    /// Evento de dominio que indica que se ha eliminado un contacto de un cliente.
    /// </summary>
    public sealed record ContactoEliminado(
        Guid ClienteId,
        Guid ContactoId
    ) : IDomainEvent
    {
        /// <summary>
        /// Fecha y hora en que ocurrió el evento (UTC).
        /// </summary>
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}

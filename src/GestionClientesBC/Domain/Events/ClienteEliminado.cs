using System;
using SharedKernel.Events;

namespace GestionClientesBC.Domain.Events
{
    /// <summary>
    /// Evento de dominio que representa la eliminación de un cliente.
    /// </summary>
    public sealed class ClienteEliminado : DomainEvent
    {
        public Guid ClienteId { get; }

        public ClienteEliminado(Guid clienteId, Guid? eventId = null, DateTime? occurredOnUtc = null)
            : base(eventId, occurredOnUtc)
        {
            ClienteId = clienteId;
        }
    }
}

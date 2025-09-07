using System;
using SharedKernel.Events;

namespace GestionClientesBC.Domain.Events
{
    /// <summary>
    /// Evento de dominio que representa la habilitación de un cliente.
    /// </summary>
    public sealed class ClienteHabilitado : DomainEvent
    {
        public Guid ClienteId { get; }

        public ClienteHabilitado(Guid clienteId, Guid? eventId = null, DateTime? occurredOnUtc = null)
            : base(eventId, occurredOnUtc)
        {
            ClienteId = clienteId;
        }
    }
}

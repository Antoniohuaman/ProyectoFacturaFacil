
using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Domain.Events
{
    /// <summary>
    /// Evento de dominio que representa la eliminación de un cliente.
    /// </summary>
    public sealed class ClienteEliminado : DomainEvent
    {
        public Guid ClienteId { get; }
        public EmpresaId EmpresaId { get; }

        public ClienteEliminado(Guid clienteId, EmpresaId empresaId, Guid? eventId = null, DateTime? occurredOnUtc = null)
            : base(eventId, occurredOnUtc)
        {
            ClienteId = clienteId;
            EmpresaId = empresaId;
        }
    }
}

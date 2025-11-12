
using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Domain.Events
{
    /// <summary>
    /// Evento de dominio que indica que se ha agregado un contacto a un cliente.
    /// </summary>
    public sealed class ContactoAgregado : DomainEvent
    {
        public Guid ClienteId { get; }
        public EmpresaId EmpresaId { get; }
        public Guid ContactoId { get; }

        public ContactoAgregado(Guid clienteId, EmpresaId empresaId, Guid contactoId, Guid? eventId = null, DateTime? occurredOnUtc = null)
            : base(eventId, occurredOnUtc)
        {
            ClienteId = clienteId;
            EmpresaId = empresaId;
            ContactoId = contactoId;
        }
    }
}
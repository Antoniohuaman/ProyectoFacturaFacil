using System;
using GestionClientesBC.Domain.Entities;

namespace GestionClientesBC.Domain.Events
{
    /// <summary>
    /// Evento que se dispara cuando se agrega un contacto a un cliente.
    /// </summary>
    public sealed class ContactoAgregado : IDomainEvent
    {
        public Guid ClienteId { get; }
        public ContactoCliente Contacto { get; }
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public ContactoAgregado(Guid clienteId, ContactoCliente contacto)
        {
            ClienteId = clienteId;
            Contacto = contacto;
        }
    }
}
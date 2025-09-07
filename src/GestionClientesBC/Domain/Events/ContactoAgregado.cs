
using System;
using GestionClientesBC.Domain.Entities;
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
        public ContactoCliente Contacto { get; }

        public ContactoAgregado(Guid clienteId, EmpresaId empresaId, ContactoCliente contacto, Guid? eventId = null, DateTime? occurredOnUtc = null)
            : base(eventId, occurredOnUtc)
        {
            ClienteId = clienteId;
            EmpresaId = empresaId;
            Contacto = contacto;
        }
    }
}
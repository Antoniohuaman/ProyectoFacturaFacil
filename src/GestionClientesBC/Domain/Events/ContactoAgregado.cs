using System;
using GestionClientesBC.Domain.Entities;
using SharedKernel.Events;

namespace GestionClientesBC.Domain.Events
{
    /// <summary>
    /// Evento de dominio que indica que se ha agregado un contacto a un cliente.
    /// </summary>
    public sealed record ContactoAgregado(
        Guid ClienteId,
        ContactoCliente Contacto
    ) : IDomainEvent
    {
        /// <summary>
        /// Fecha y hora en que ocurrió el evento (UTC).
        /// </summary>
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
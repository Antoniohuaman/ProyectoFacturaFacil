
using System;
using GestionClientesBC.Domain.Entities;
using SharedKernel.Events;

namespace GestionClientesBC.Domain.Events
{
    /// <summary>
    /// Evento de dominio que indica que se ha agregado un adjunto a un cliente.
    /// </summary>
    public sealed record AdjuntoAgregado(
        Guid ClienteId,
        AdjuntoCliente Adjunto
    ) : IDomainEvent
    {
        /// <summary>
        /// Fecha y hora en que ocurrió el evento (UTC).
        /// </summary>
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
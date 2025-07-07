using System;
using GestionClientesBC.Domain.Entities;

namespace GestionClientesBC.Domain.Events
{
    /// <summary>
    /// Evento que se dispara cuando se agrega un adjunto a un cliente.
    /// </summary>
    public sealed class AdjuntoAgregado : IDomainEvent
    {
        public Guid ClienteId { get; }
        public AdjuntoCliente Adjunto { get; }
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public AdjuntoAgregado(Guid clienteId, AdjuntoCliente adjunto)
        {
            ClienteId = clienteId;
            Adjunto = adjunto;
        }
    }
}
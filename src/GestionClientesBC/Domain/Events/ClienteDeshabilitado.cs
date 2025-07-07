using System;

namespace GestionClientesBC.Domain.Events
{
    /// <summary>
    /// Evento que se dispara cuando un cliente es deshabilitado.
    /// </summary>
    public sealed class ClienteDeshabilitado : IDomainEvent
    {
        public Guid ClienteId { get; }
        public string Motivo { get; }
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public ClienteDeshabilitado(Guid clienteId, string motivo)
        {
            ClienteId = clienteId;
            Motivo = motivo;
        }
    }
}
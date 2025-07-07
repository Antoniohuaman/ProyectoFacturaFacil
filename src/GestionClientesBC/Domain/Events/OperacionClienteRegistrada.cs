using System;
using GestionClientesBC.Domain.Entities;

namespace GestionClientesBC.Domain.Events
{
    /// <summary>
    /// Evento que se dispara cuando se registra una operación en el historial del cliente.
    /// </summary>
    public sealed class OperacionClienteRegistrada : IDomainEvent
    {
        public Guid ClienteId { get; }
        public OperacionCliente Operacion { get; }
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public OperacionClienteRegistrada(Guid clienteId, OperacionCliente operacion)
        {
            ClienteId = clienteId;
            Operacion = operacion;
        }
    }
}
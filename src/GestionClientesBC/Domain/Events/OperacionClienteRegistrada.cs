using System;
using GestionClientesBC.Domain.Entities;
using SharedKernel.Events;

namespace GestionClientesBC.Domain.Events
{
    /// <summary>
    /// Evento de dominio que indica que se ha registrado una operación en el historial del cliente.
    /// </summary>
    public sealed record OperacionClienteRegistrada(
        Guid ClienteId,
        OperacionCliente Operacion
    ) : IDomainEvent
    {
        /// <summary>
        /// Fecha y hora en que ocurrió el evento (UTC).
        /// </summary>
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
using System;
using System.Collections.Generic;

namespace GestionClientesBC.Domain.Events
{
    /// <summary>
    /// Evento que se dispara cuando se modifica un cliente.
    /// </summary>
    public sealed class ClienteModificado : IDomainEvent
    {
        public Guid ClienteId { get; }
        public IReadOnlyDictionary<string, (object? Anterior, object? Nuevo)> Cambios { get; }
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public ClienteModificado(Guid clienteId, IReadOnlyDictionary<string, (object?, object?)> cambios)
        {
            ClienteId = clienteId;
            Cambios = cambios;
        }
    }
}
using System;
using System.Collections.Generic;

namespace GestionClientesBC.Domain.Events
{
    public class ClienteModificado : IDomainEvent
    {
        public Guid ClienteId { get; }
        public IDictionary<string, (object? anterior, object? nuevo)>? Cambios { get; }
        public DateTime FechaModificacion { get; }
        public DateTime OccurredOn => FechaModificacion;

        // Constructor para cambios detallados
        public ClienteModificado(Guid clienteId, IDictionary<string, (object? anterior, object? nuevo)> cambios, DateTime fechaModificacion)
        {
            ClienteId = clienteId;
            Cambios = cambios;
            FechaModificacion = fechaModificacion;
        }

        // Constructor simple (opcional, para otros casos de uso)
        public ClienteModificado(Guid clienteId, DateTime fechaModificacion)
        {
            ClienteId = clienteId;
            Cambios = null;
            FechaModificacion = fechaModificacion;
        }
    }
}
using System;
using System.Collections.Generic;

namespace GestionClientesBC.Domain.Events
{
    public class ClienteModificado : IDomainEvent
    {
        public Guid ClienteId { get; }
        public IDictionary<string, (object? anterior, object? nuevo)> Cambios { get; }
        public DateTime Fecha { get; }
        public DateTime OccurredOn => Fecha; // Implementación de la interfaz

        public ClienteModificado(Guid clienteId, IDictionary<string, (object? anterior, object? nuevo)> cambios, DateTime fecha)
        {
            ClienteId = clienteId;
            Cambios = cambios;
            Fecha = fecha;
        }
    }
}
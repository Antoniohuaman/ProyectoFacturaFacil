using System;

namespace GestionClientesBC.Domain.Events
{
    public class ClienteDeshabilitado : IDomainEvent
    {
        public Guid ClienteId { get; }
        public string? Motivo { get; }
        public DateTime Fecha { get; }
        public DateTime OccurredOn => Fecha;

        public ClienteDeshabilitado(Guid clienteId, string? motivo, DateTime fecha)
        {
            ClienteId = clienteId;
            Motivo = motivo;
            Fecha = fecha;
        }
    }
}
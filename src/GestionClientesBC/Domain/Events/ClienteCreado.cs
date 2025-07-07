using System;
using GestionClientesBC.Domain.ValueObjects;

namespace GestionClientesBC.Domain.Events
{
    /// <summary>
    /// Evento que se dispara cuando se crea un cliente.
    /// </summary>
    public sealed class ClienteCreado : IDomainEvent
    {
        public Guid ClienteId { get; }
        public DocumentoIdentidad DocumentoIdentidad { get; }
        public string Correo { get; }
        public TipoCliente TipoCliente { get; }
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public ClienteCreado(Guid clienteId, DocumentoIdentidad documentoIdentidad, string correo, TipoCliente tipoCliente)
        {
            ClienteId = clienteId;
            DocumentoIdentidad = documentoIdentidad;
            Correo = correo;
            TipoCliente = tipoCliente;
        }
    }
}


using System;
using GestionClientesBC.Domain.Entities;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Domain.Events
{
    /// <summary>
    /// Evento de dominio que indica que se ha agregado un adjunto a un cliente.
    /// </summary>
    /// <summary>
    /// Evento de dominio que indica que se ha agregado un adjunto a un cliente.
    /// </summary>
    public sealed class AdjuntoAgregado : DomainEvent
    {
        public Guid ClienteId { get; }
        public EmpresaId EmpresaId { get; }
        public AdjuntoCliente Adjunto { get; }

        public AdjuntoAgregado(Guid clienteId, EmpresaId empresaId, AdjuntoCliente adjunto, Guid? eventId = null, DateTime? occurredOnUtc = null)
            : base(eventId, occurredOnUtc)
        {
            ClienteId = clienteId;
            EmpresaId = empresaId;
            Adjunto = adjunto;
        }
    }
}
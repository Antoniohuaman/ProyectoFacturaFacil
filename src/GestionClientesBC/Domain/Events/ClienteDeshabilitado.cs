
using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Domain.Events
{
    /// <summary>
    /// Evento de dominio que indica que un cliente ha sido deshabilitado.
    /// </summary>
    public sealed class ClienteDeshabilitado : DomainEvent
    {
        public Guid ClienteId { get; }
        public EmpresaId EmpresaId { get; }
        public string? Motivo { get; }
        public DateTime Fecha { get; }

        public ClienteDeshabilitado(Guid clienteId, EmpresaId empresaId, string? motivo, DateTime fecha, Guid? eventId = null, DateTime? occurredOnUtc = null)
            : base(eventId, occurredOnUtc)
        {
            ClienteId = clienteId;
            EmpresaId = empresaId;
            Motivo = motivo;
            Fecha = fecha;
        }
    }
}
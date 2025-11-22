using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace ListaPreciosBC.Domain.Events
{
    /// <summary>
    /// Se emite cuando un paquete se elimina del catálogo de precios.
    /// </summary>
    public sealed class PaqueteEliminado : DomainEvent
    {
        public EmpresaId EmpresaId { get; }
        public Guid PaqueteId { get; }

        public PaqueteEliminado(
            EmpresaId empresaId,
            Guid paqueteId,
            Guid? eventId = null,
            DateTime? occurredOnUtc = null)
            : base(eventId, occurredOnUtc)
        {
            EmpresaId = empresaId;
            PaqueteId = paqueteId;
        }
    }
}

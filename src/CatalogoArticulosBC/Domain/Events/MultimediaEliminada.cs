
using SharedKernel.Events;
using System;

namespace CatalogoArticulosBC.Domain.Events
{
    public class MultimediaEliminada : DomainEvent
    {
        public Guid ProductoId { get; }
        public Guid MultimediaId { get; }
        public MultimediaEliminada(Guid productoId, Guid multimediaId, Guid? eventId = null, DateTime? occurredOnUtc = null)
            : base(eventId, occurredOnUtc)
        {
            ProductoId = productoId;
            MultimediaId = multimediaId;
        }
    }
}

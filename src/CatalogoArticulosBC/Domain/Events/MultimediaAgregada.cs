
using SharedKernel.Events;
using System;

namespace CatalogoArticulosBC.Domain.Events
{
    public class MultimediaAgregada : DomainEvent
    {
        public Guid ProductoId { get; }
        public Guid MultimediaId { get; }
        public MultimediaAgregada(Guid productoId, Guid multimediaId, Guid? eventId = null, DateTime? occurredOnUtc = null)
            : base(eventId, occurredOnUtc)
        {
            ProductoId = productoId;
            MultimediaId = multimediaId;
        }
    }
}

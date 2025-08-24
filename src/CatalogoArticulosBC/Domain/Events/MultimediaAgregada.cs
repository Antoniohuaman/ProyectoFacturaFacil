using SharedKernel.Events;
using System;

namespace CatalogoArticulosBC.Domain.Events
{
    public class MultimediaAgregada : IDomainEvent
    {
        public Guid ProductoId { get; }
        public Guid MultimediaId { get; }
        public DateTime Fecha { get; }
        public MultimediaAgregada(Guid productoId, Guid multimediaId)
        {
            ProductoId = productoId;
            MultimediaId = multimediaId;
            Fecha = DateTime.UtcNow;
        }
    }
}

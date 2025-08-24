using SharedKernel.Events;
using System;

namespace CatalogoArticulosBC.Domain.Events
{
    public class MultimediaEliminada : IDomainEvent
    {
        public Guid ProductoId { get; }
        public Guid MultimediaId { get; }
        public DateTime Fecha { get; }
        public MultimediaEliminada(Guid productoId, Guid multimediaId)
        {
            ProductoId = productoId;
            MultimediaId = multimediaId;
            Fecha = DateTime.UtcNow;
        }
    }
}

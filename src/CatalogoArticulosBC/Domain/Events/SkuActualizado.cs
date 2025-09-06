using SharedKernel.Events;
using SharedKernel.ValueObjects;
using System;

namespace CatalogoArticulosBC.Domain.Events
{
    public class SkuActualizado : DomainEvent
    {
        public Guid ProductoId { get; }
        public Sku NuevoSku { get; }

        public SkuActualizado(Guid productoId, Sku nuevoSku, Guid? eventId = null, DateTime? occurredOnUtc = null)
            : base(eventId, occurredOnUtc)
        {
            ProductoId = productoId;
            NuevoSku = nuevoSku;
        }
    }
}

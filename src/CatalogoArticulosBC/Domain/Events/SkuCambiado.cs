using SharedKernel.Events;
using SharedKernel.ValueObjects;
using System;

namespace CatalogoArticulosBC.Domain.Events
{
    public class SkuCambiado : IDomainEvent
    {
        public Guid ProductoId { get; }
        public Sku NuevoSku { get; }
        public DateTime Fecha { get; }
        public SkuCambiado(Guid productoId, Sku nuevoSku)
        {
            ProductoId = productoId;
            NuevoSku = nuevoSku;
            Fecha = DateTime.UtcNow;
        }
    }
}

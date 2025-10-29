using SharedKernel.Events;
using SharedKernel.ValueObjects;
using System;

namespace CatalogoArticulosBC.Domain.Events
{
    public class SkuActualizado : DomainEvent
    {
        public Guid ProductoId { get; }
        public EmpresaId EmpresaId { get; }
        public Sku NuevoSku { get; }

        public SkuActualizado(Guid productoId, EmpresaId empresaId, Sku nuevoSku, Guid? eventId = null, DateTime? occurredOnUtc = null)
            : base(eventId, occurredOnUtc)
        {
            ProductoId = productoId;
            EmpresaId = empresaId;
            NuevoSku = nuevoSku;
        }
    }
}

using SharedKernel.Events;
using System;
using System.Collections.Generic;

namespace CatalogoArticulosBC.Domain.Events
{
    public class ProductosActualizadosMasivamente : DomainEvent
    {
        public IReadOnlyList<Guid> ProductoIds { get; }
        public int Cantidad { get; }
        public string Usuario { get; }
        public ProductosActualizadosMasivamente(IReadOnlyList<Guid> productoIds, int cantidad, string usuario, Guid? eventId = null, DateTime? occurredOnUtc = null)
            : base(eventId, occurredOnUtc)
        {
            ProductoIds = productoIds;
            Cantidad = cantidad;
            Usuario = usuario;
        }
    }
}

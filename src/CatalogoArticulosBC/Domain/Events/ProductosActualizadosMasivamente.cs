using SharedKernel.Events;
using System;
using System.Collections.Generic;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Domain.Events
{
    public class ProductosActualizadosMasivamente : DomainEvent
    {
        public IReadOnlyList<Guid> ProductoIds { get; }
        public EmpresaId EmpresaId { get; }
        public int Cantidad { get; }
        public string Usuario { get; }
        public ProductosActualizadosMasivamente(IReadOnlyList<Guid> productoIds, EmpresaId empresaId, int cantidad, string usuario, Guid? eventId = null, DateTime? occurredOnUtc = null)
            : base(eventId, occurredOnUtc)
        {
            ProductoIds = productoIds;
            EmpresaId = empresaId;
            Cantidad = cantidad;
            Usuario = usuario;
        }
    }
}

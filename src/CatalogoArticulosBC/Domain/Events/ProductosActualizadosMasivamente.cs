using SharedKernel.Events;
using System;
using System.Collections.Generic;

namespace CatalogoArticulosBC.Domain.Events
{
    public class ProductosActualizadosMasivamente : IDomainEvent
    {
        public IReadOnlyList<Guid> ProductoIds { get; }
        public int Cantidad { get; }
        public string Usuario { get; }
        public DateTime Fecha { get; }

        public ProductosActualizadosMasivamente(IReadOnlyList<Guid> productoIds, int cantidad, string usuario, DateTime fecha)
        {
            ProductoIds = productoIds;
            Cantidad = cantidad;
            Usuario = usuario;
            Fecha = fecha;
        }
    }
}

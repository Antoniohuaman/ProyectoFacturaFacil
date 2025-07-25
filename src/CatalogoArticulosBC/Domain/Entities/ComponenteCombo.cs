using System;

namespace CatalogoArticulosBC.Domain.Entities
{
    public sealed class ComponenteCombo
    {
        public Guid ProductoId { get; }
        public int Cantidad    { get; }

        public ComponenteCombo(Guid productoId, int cantidad)
        {
            if (productoId == Guid.Empty)
                throw new ArgumentException("ProductoId inválido.", nameof(productoId));
            if (cantidad <= 0)
                throw new ArgumentException("La cantidad debe ser mayor a cero.", nameof(cantidad));

            ProductoId = productoId;
            Cantidad = cantidad;
        }
    }
}
// src/CatalogoArticulosBC/Domain/Entities/ComponenteCombo.cs
using System;

namespace CatalogoArticulosBC.Domain.Entities
{
    public sealed class ComponenteCombo
    {
        public Guid ProductoId { get; }
        public int Cantidad    { get; }

        public ComponenteCombo(Guid productoId, int cantidad)
        {
            ProductoId = productoId != Guid.Empty
                ? productoId
                : throw new ArgumentException("ProductoId inválido.", nameof(productoId));
            Cantidad = cantidad > 0
                ? cantidad
                : throw new ArgumentException("La cantidad debe ser al menos 1.", nameof(cantidad));
        }
    }
}
// src/CatalogoArticulosBC/Domain/Events/ProductoModificado.cs
using System;

namespace CatalogoArticulosBC.Domain.Events
{
    public sealed class ProductoModificado : IDomainEvent
    {
        public Guid ProductoId { get; }
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public ProductoModificado(Guid productoId)
        {
            ProductoId = productoId;
        }
    }
}   
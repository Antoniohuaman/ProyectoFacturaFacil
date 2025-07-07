// src/CatalogoArticulosBC/Domain/Events/ProductoCreado.cs
using System;
using CatalogoArticulosBC.Domain.ValueObjects;

namespace CatalogoArticulosBC.Domain.Events
{
    public sealed class ProductoCreado : IDomainEvent
    {
        public Guid ProductoId { get; }
        public SKU Sku         { get; }
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public ProductoCreado(Guid productoId, SKU sku)
        {
            ProductoId = productoId;
            Sku = sku ?? throw new ArgumentNullException(nameof(sku));
        }
    }
}
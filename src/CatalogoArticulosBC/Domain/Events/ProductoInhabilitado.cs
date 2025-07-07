// src/CatalogoArticulosBC/Domain/Events/ProductoInhabilitado.cs
using System;

namespace CatalogoArticulosBC.Domain.Events
{
    public sealed class ProductoInhabilitado : IDomainEvent
    {
        public Guid ProductoId { get; }
        public string Motivo   { get; }
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public ProductoInhabilitado(Guid productoId, string motivo)
        {
            ProductoId = productoId;
            Motivo = motivo ?? string.Empty;
        }
    }
}
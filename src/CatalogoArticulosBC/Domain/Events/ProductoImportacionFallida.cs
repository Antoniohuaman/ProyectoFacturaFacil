using SharedKernel.Events;
using System;

namespace CatalogoArticulosBC.Domain.Events
{
    public class ProductoImportacionFallida : DomainEvent
    {
        public string Sku { get; }
        public string Motivo { get; }
        public string Usuario { get; }
        public ProductoImportacionFallida(string sku, string motivo, string usuario, Guid? eventId = null, DateTime? occurredOnUtc = null)
            : base(eventId, occurredOnUtc)
        {
            Sku = sku;
            Motivo = motivo;
            Usuario = usuario;
        }
    }
}

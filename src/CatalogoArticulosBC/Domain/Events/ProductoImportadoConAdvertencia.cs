using SharedKernel.Events;
using System;

namespace CatalogoArticulosBC.Domain.Events
{
    public class ProductoImportadoConAdvertencia : DomainEvent
    {
        public string Sku { get; }
        public string Advertencia { get; }
        public string Usuario { get; }
        public ProductoImportadoConAdvertencia(string sku, string advertencia, string usuario, Guid? eventId = null, DateTime? occurredOnUtc = null)
            : base(eventId, occurredOnUtc)
        {
            Sku = sku;
            Advertencia = advertencia;
            Usuario = usuario;
        }
    }
}

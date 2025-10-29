using SharedKernel.Events;
using System;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Domain.Events
{
    public class ProductoImportadoConAdvertencia : DomainEvent
    {
        public string Sku { get; }
        public EmpresaId EmpresaId { get; }
        public string Advertencia { get; }
        public string Usuario { get; }
        public ProductoImportadoConAdvertencia(string sku, EmpresaId empresaId, string advertencia, string usuario, Guid? eventId = null, DateTime? occurredOnUtc = null)
            : base(eventId, occurredOnUtc)
        {
            Sku = sku;
            EmpresaId = empresaId;
            Advertencia = advertencia;
            Usuario = usuario;
        }
    }
}

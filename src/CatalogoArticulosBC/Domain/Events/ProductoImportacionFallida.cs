using SharedKernel.Events;
using System;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Domain.Events
{
    public class ProductoImportacionFallida : DomainEvent
    {
        public string Sku { get; }
        public EmpresaId EmpresaId { get; }
        public string Motivo { get; }
        public string Usuario { get; }
        public ProductoImportacionFallida(string sku, EmpresaId empresaId, string motivo, string usuario, Guid? eventId = null, DateTime? occurredOnUtc = null)
            : base(eventId, occurredOnUtc)
        {
            Sku = sku;
            EmpresaId = empresaId;
            Motivo = motivo;
            Usuario = usuario;
        }
    }
}

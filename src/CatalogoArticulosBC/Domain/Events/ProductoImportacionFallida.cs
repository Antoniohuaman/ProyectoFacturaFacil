using SharedKernel.Events;
using System;

namespace CatalogoArticulosBC.Domain.Events
{
    public class ProductoImportacionFallida : IDomainEvent
    {
        public string Sku { get; }
        public string Motivo { get; }
        public string Usuario { get; }
        public DateTime Fecha { get; }

        public ProductoImportacionFallida(string sku, string motivo, string usuario, DateTime fecha)
        {
            Sku = sku;
            Motivo = motivo;
            Usuario = usuario;
            Fecha = fecha;
        }
    }
}

using SharedKernel.Events;
using System;

namespace CatalogoArticulosBC.Domain.Events
{
    public class ProductoImportadoConAdvertencia : IDomainEvent
    {
        public string Sku { get; }
        public string Advertencia { get; }
        public string Usuario { get; }
        public DateTime Fecha { get; }

        public ProductoImportadoConAdvertencia(string sku, string advertencia, string usuario, DateTime fecha)
        {
            Sku = sku;
            Advertencia = advertencia;
            Usuario = usuario;
            Fecha = fecha;
        }
    }
}

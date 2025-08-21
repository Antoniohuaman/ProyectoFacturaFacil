using System;
using SharedKernel.Events;
using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Domain.Events
{
    public sealed class PrecioBaseVigenteEstablecido : DomainEvent
    {
        public string Sku { get; }
        public IdentificadorColumnaPrecio ColumnaBase { get; }
        public PrecioResuelto Precio { get; }
        public DateTimeOffset OcurrioEn { get; }

        public PrecioBaseVigenteEstablecido(
            string sku,
            IdentificadorColumnaPrecio columnaBase,
            PrecioResuelto precio,
            DateTimeOffset ocurrioEn)
            : base(occurredOnUtc: ocurrioEn.UtcDateTime)
        {
            Sku = sku;
            ColumnaBase = columnaBase;
            Precio = precio;
            OcurrioEn = ocurrioEn;
        }
    }
}

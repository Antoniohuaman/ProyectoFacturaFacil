using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects;
using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Domain.Events
{
    public sealed class PrecioBaseVigenteEstablecido : DomainEvent
    {
        public Sku Sku { get; }
        public IdentificadorColumnaPrecio ColumnaBase { get; }
        public PrecioResuelto Precio { get; }
        public DateTimeOffset OcurrioEn { get; }

        public PrecioBaseVigenteEstablecido(
            Sku sku,
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

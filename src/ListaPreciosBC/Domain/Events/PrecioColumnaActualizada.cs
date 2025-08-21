using System;
using SharedKernel.Events;
using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Domain.Events
{
    public sealed class PrecioColumnaActualizada : DomainEvent
    {
        public string Sku { get; }
        public IdentificadorColumnaPrecio Columna { get; }
        public DateTimeOffset OcurrioEn { get; }

        public PrecioColumnaActualizada(string sku, IdentificadorColumnaPrecio columna, DateTimeOffset ocurrioEn)
            : base(occurredOnUtc: ocurrioEn.UtcDateTime)
        {
            Sku = sku; Columna = columna; OcurrioEn = ocurrioEn;
        }
    }
}

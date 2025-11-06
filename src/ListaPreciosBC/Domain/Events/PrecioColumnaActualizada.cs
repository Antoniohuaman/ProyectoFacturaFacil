using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects;
using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Domain.Events
{
    public sealed class PrecioColumnaActualizada : DomainEvent
    {
        public EmpresaId EmpresaId { get; }
        public ProductoId ProductoId { get; }
        public IdentificadorColumnaPrecio Columna { get; }
        public DateTimeOffset OcurrioEn { get; }

        public PrecioColumnaActualizada(EmpresaId empresaId, ProductoId productoId, IdentificadorColumnaPrecio columna, DateTimeOffset ocurrioEn)
            : base(occurredOnUtc: ocurrioEn.UtcDateTime)
        {
            EmpresaId = empresaId;
            ProductoId = productoId;
            Columna = columna;
            OcurrioEn = ocurrioEn;
        }
    }
}

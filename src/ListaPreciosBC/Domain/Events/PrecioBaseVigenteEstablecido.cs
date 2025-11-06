using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects;
using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Domain.Events
{
    public sealed class PrecioBaseVigenteEstablecido : DomainEvent
    {
        public EmpresaId EmpresaId { get; }
        public ProductoId ProductoId { get; }
        public IdentificadorColumnaPrecio ColumnaBase { get; }
        public PrecioResuelto Precio { get; }
        public DateTimeOffset OcurrioEn { get; }

        public PrecioBaseVigenteEstablecido(
            EmpresaId empresaId,
            ProductoId productoId,
            IdentificadorColumnaPrecio columnaBase,
            PrecioResuelto precio,
            DateTimeOffset ocurrioEn)
            : base(occurredOnUtc: ocurrioEn.UtcDateTime)
        {
            EmpresaId = empresaId;
            ProductoId = productoId;
            ColumnaBase = columnaBase;
            Precio = precio;
            OcurrioEn = ocurrioEn;
        }
    }
}

using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects;
using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Domain.Events
{
    public sealed class MatrizVolumenActualizada : DomainEvent
    {
        public EmpresaId EmpresaId { get; }
        public ProductoId ProductoId { get; }
        public IdentificadorColumnaPrecio Columna { get; }
        public DateTimeOffset OcurrioEn { get; }

        public MatrizVolumenActualizada(EmpresaId empresaId, ProductoId productoId, IdentificadorColumnaPrecio columna, DateTimeOffset ocurrioEn)
            : base(occurredOnUtc: ocurrioEn.UtcDateTime)
        {
            EmpresaId = empresaId;
            ProductoId = productoId;
            Columna = columna;
            OcurrioEn = ocurrioEn;
        }
    }
}

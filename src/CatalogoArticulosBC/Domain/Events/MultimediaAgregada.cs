
using SharedKernel.Events;
using System;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Domain.Events
{
    public class MultimediaAgregada : DomainEvent
    {
        public Guid ProductoId { get; }
        public EmpresaId EmpresaId { get; }
        public Guid MultimediaId { get; }
        public MultimediaAgregada(Guid productoId, EmpresaId empresaId, Guid multimediaId, Guid? eventId = null, DateTime? occurredOnUtc = null)
            : base(eventId, occurredOnUtc)
        {
            ProductoId = productoId;
            EmpresaId = empresaId;
            MultimediaId = multimediaId;
        }
    }
}

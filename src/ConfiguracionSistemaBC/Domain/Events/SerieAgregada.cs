using System;
using SharedKernel.ValueObjects;
using ConfiguracionSistemaBC.Domain.ValueObjects;

namespace ConfiguracionSistemaBC.Domain.Events
{
    using SharedKernel.Events;

    public class SerieAgregada : DomainEvent
    {
        public EmpresaId EmpresaId { get; }
        public string SerieCodigo { get; }

        public SerieAgregada(EmpresaId empresaId, string serieCodigo, DateTime? occurredOnUtc = null, System.Guid? eventId = null)
            : base(eventId, occurredOnUtc)
        {
            EmpresaId = empresaId;
            SerieCodigo = serieCodigo;
        }
    }
}

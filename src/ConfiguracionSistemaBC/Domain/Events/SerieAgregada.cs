using System;

namespace ConfiguracionSistemaBC.Domain.Events
{
    using SharedKernel.Events;

    public class SerieAgregada : DomainEvent
    {
        public string EmpresaCodigo { get; }
        public string SerieCodigo { get; }

        public SerieAgregada(string empresaCodigo, string serieCodigo, DateTime? occurredOnUtc = null, System.Guid? eventId = null)
            : base(eventId, occurredOnUtc)
        {
            EmpresaCodigo = empresaCodigo;
            SerieCodigo = serieCodigo;
        }
    }
}

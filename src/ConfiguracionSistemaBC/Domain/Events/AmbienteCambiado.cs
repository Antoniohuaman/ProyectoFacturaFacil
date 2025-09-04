using System;

namespace ConfiguracionSistemaBC.Domain.Events
{
    using SharedKernel.Events;

    public class AmbienteCambiado : DomainEvent
    {
        public string EmpresaCodigo { get; }
        public string De { get; }
        public string A { get; }

        public AmbienteCambiado(string empresaCodigo, string de, string a, DateTime? occurredOnUtc = null, System.Guid? eventId = null)
            : base(eventId, occurredOnUtc)
        {
            EmpresaCodigo = empresaCodigo;
            De = de;
            A = a;
        }
    }
}

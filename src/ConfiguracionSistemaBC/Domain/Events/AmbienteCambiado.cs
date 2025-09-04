using System;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;
namespace ConfiguracionSistemaBC.Domain.Events
{
    using SharedKernel.Events;

    public class AmbienteCambiado : DomainEvent
    {
        public EmpresaId EmpresaId { get; }
        public string De { get; }
        public string A { get; }

        public AmbienteCambiado(EmpresaId empresaId, string de, string a, DateTime? occurredOnUtc = null, System.Guid? eventId = null)
            : base(eventId, occurredOnUtc)
        {
            EmpresaId = empresaId;
            De = de;
            A = a;
        }
    }
}

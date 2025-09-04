using System;
using SharedKernel.ValueObjects;
using ConfiguracionSistemaBC.Domain.ValueObjects;

namespace ConfiguracionSistemaBC.Domain.Events
{
    using SharedKernel.Events;

    public class EstablecimientoRegistrado : DomainEvent
    {
        public EmpresaId EmpresaId { get; }
        public string EstablecimientoCodigo { get; }

        public EstablecimientoRegistrado(EmpresaId empresaId, string establecimientoCodigo, DateTime? occurredOnUtc = null, System.Guid? eventId = null)
            : base(eventId, occurredOnUtc)
        {
            EmpresaId = empresaId;
            EstablecimientoCodigo = establecimientoCodigo;
        }
    }
}

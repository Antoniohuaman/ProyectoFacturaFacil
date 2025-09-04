using System;

namespace ConfiguracionSistemaBC.Domain.Events
{
    using SharedKernel.Events;

    public class EstablecimientoRegistrado : DomainEvent
    {
        public string EmpresaCodigo { get; }
        public string EstablecimientoCodigo { get; }

        public EstablecimientoRegistrado(string empresaCodigo, string establecimientoCodigo, DateTime? occurredOnUtc = null, System.Guid? eventId = null)
            : base(eventId, occurredOnUtc)
        {
            EmpresaCodigo = empresaCodigo;
            EstablecimientoCodigo = establecimientoCodigo;
        }
    }
}

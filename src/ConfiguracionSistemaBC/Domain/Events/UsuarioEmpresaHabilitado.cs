using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects;
using ConfiguracionSistemaBC.Domain.ValueObjects;

namespace ConfiguracionSistemaBC.Domain.Events
{
    public sealed class UsuarioEmpresaHabilitado : DomainEvent
    {
        public EmpresaId EmpresaId { get; }
        public IReadOnlyCollection<EstablecimientoId> Establecimientos { get; }

        public UsuarioEmpresaHabilitado(
            EmpresaId empresaId,
            IReadOnlyCollection<EstablecimientoId> establecimientos,
            DateTime? occurredOnUtc = null,
            Guid? eventId = null)
            : base(eventId, occurredOnUtc)
        {
            EmpresaId = empresaId;
            Establecimientos = establecimientos;
        }
    }
}

using SharedKernel.ValueObjects;
using SharedKernel.Events;
using System;
using System.Collections.Generic;

namespace ConfiguracionSistemaBC.Domain.Events
{
    /// <summary>
    /// Evento publicado cuando se actualizan los accesos de usuario a establecimientos.
    /// </summary>
    public sealed class AccesosDeUsuarioEmpresaActualizados : DomainEvent
    {
        public EmpresaId EmpresaId { get; }
        public UsuarioId UsuarioId { get; }
        public IReadOnlyCollection<(EstablecimientoId EstablecimientoId, IReadOnlyCollection<Guid> RolIds)> Accesos { get; }

        public AccesosDeUsuarioEmpresaActualizados(
            EmpresaId empresaId,
            UsuarioId usuarioId,
            IReadOnlyCollection<(EstablecimientoId EstablecimientoId, IReadOnlyCollection<Guid> RolIds)> accesos,
            DateTime? occurredOnUtc = null,
            Guid? eventId = null)
            : base(eventId, occurredOnUtc)
        {
            EmpresaId = empresaId;
            UsuarioId = usuarioId;
            Accesos = accesos;
        }
    }
}

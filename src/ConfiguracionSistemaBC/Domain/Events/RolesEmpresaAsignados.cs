using SharedKernel.ValueObjects;
using SharedKernel.Events;
using System;
using System.Collections.Generic;

namespace ConfiguracionSistemaBC.Domain.Events
{
    /// <summary>
    /// Evento publicado cuando se asignan roles de empresa a un usuario.
    /// </summary>
    public sealed class RolesEmpresaAsignados : DomainEvent
    {
        public EmpresaId EmpresaId { get; }
        public UsuarioId UsuarioId { get; }
        public IReadOnlyCollection<Guid> RolesEmpresaIds { get; }

        public RolesEmpresaAsignados(
            EmpresaId empresaId,
            UsuarioId usuarioId,
            IReadOnlyCollection<Guid> rolesEmpresaIds,
            DateTime? occurredOnUtc = null,
            Guid? eventId = null)
            : base(eventId, occurredOnUtc)
        {
            EmpresaId = empresaId;
            UsuarioId = usuarioId;
            RolesEmpresaIds = rolesEmpresaIds;
        }
    }
}

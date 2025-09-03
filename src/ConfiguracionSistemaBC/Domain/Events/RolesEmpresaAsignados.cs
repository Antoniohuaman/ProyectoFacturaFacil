using SharedKernel.Events;
using SharedKernel.ValueObjects;
using System;
using System.Collections.Generic;

namespace ConfiguracionSistemaBC.Domain.Events
{
    /// <summary>
    /// Evento publicado cuando se asignan roles de empresa a un usuario.
    /// </summary>
    public record RolesEmpresaAsignados(
        EmpresaId EmpresaId,
        UsuarioId UsuarioId,
        IReadOnlyCollection<Guid> RolesEmpresaIds
    ) : IDomainEvent;
}

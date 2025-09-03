using SharedKernel.Events;
using SharedKernel.ValueObjects;
using System;
using System.Collections.Generic;

namespace ConfiguracionSistemaBC.Domain.Events
{
    /// <summary>
    /// Evento publicado cuando se actualizan los accesos de usuario a establecimientos.
    /// </summary>
    public record AccesosDeUsuarioEmpresaActualizados(
        EmpresaId EmpresaId,
        UsuarioId UsuarioId,
        IReadOnlyCollection<(EstablecimientoId EstablecimientoId, IReadOnlyCollection<Guid> RolIds)> Accesos
    ) : IDomainEvent;
}

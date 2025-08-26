using System;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using SharedKernel.Events;

namespace ConfiguracionSistemaBC.Domain.Events
{
    public sealed record UsuarioEmpleadoHabilitado(
        Guid UsuarioEmpleadoId,
        EmpresaId EmpresaId,
        IReadOnlyCollection<EstablecimientoId> Establecimientos
    ) : IDomainEvent;
}

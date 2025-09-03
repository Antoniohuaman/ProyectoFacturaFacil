using System;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using SharedKernel.Events;
using SharedKernel.ValueObjects;
namespace ConfiguracionSistemaBC.Domain.Events
{
    public sealed record UsuarioEmpresaHabilitado(
        EmpresaId EmpresaId,
        IReadOnlyCollection<EstablecimientoId> Establecimientos
    ) : IDomainEvent;
}

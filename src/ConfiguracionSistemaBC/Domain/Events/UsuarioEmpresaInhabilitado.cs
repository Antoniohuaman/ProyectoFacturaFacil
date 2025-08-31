using System;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Domain.Events
{
    public sealed record UsuarioEmpresaInhabilitado(
        Guid UsuarioEmpresaId,
        EmpresaId EmpresaId,
        IReadOnlyCollection<EstablecimientoId> Establecimientos,
        string Razon
    ) : IDomainEvent;
}

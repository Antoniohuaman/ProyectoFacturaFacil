using System;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using SharedKernel.Events;

namespace ConfiguracionSistemaBC.Domain.Events
{
    public sealed record UsuarioEmpleadoInhabilitado(
        Guid UsuarioEmpleadoId,
        EmpresaId EmpresaId,
        string Razon
    ) : IDomainEvent;
}



using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Domain.Events
{
    public sealed record UsuarioEmpresaInhabilitado(
        EmpresaId EmpresaId,
        UsuarioId UsuarioId,
        string Razon
    ) : IDomainEvent;
}


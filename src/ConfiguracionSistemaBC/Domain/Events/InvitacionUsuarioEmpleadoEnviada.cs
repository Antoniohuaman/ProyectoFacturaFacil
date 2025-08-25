using System;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using SharedKernel.Events;

namespace ConfiguracionSistemaBC.Domain.Events
{
    public sealed record InvitacionUsuarioEmpleadoEnviada(
        Guid UsuarioEmpleadoId,
        EmpresaId EmpresaId,
        CorreoElectronico Email,
        string Token,
        DateTime ExpiraElUtc
    ) : IDomainEvent;
}

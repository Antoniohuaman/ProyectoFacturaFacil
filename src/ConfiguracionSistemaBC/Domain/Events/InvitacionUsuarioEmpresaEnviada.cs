using System;
using SharedKernel.ValueObjects;
using SharedKernel.Events;
using ConfiguracionSistemaBC.Domain.ValueObjects;

namespace ConfiguracionSistemaBC.Domain.Events
{
    public sealed record InvitacionUsuarioEmpresaEnviada(
        EmpresaId EmpresaId,
        Email Email,
        string Token,
        DateTime ExpiraElUtc,
        IReadOnlyCollection<EstablecimientoId> Establecimientos
    ) : IDomainEvent;
}

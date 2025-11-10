using System;
using SharedKernel.ValueObjects;
using SharedKernel.Events;
using ConfiguracionSistemaBC.Domain.ValueObjects;

namespace ConfiguracionSistemaBC.Domain.Events
{
    public sealed class InvitacionUsuarioEmpresaEnviada : DomainEvent
    {
        public EmpresaId EmpresaId { get; }
        public Email Email { get; }
        public string Token { get; }
        public DateTime ExpiraElUtc { get; }
        public IReadOnlyCollection<EstablecimientoId> Establecimientos { get; }

        public InvitacionUsuarioEmpresaEnviada(
            EmpresaId empresaId,
            Email email,
            string token,
            DateTime expiraElUtc,
            IReadOnlyCollection<EstablecimientoId> establecimientos,
            DateTime? occurredOnUtc = null,
            Guid? eventId = null)
            : base(eventId, occurredOnUtc)
        {
            EmpresaId = empresaId;
            Email = email;
            Token = token;
            ExpiraElUtc = expiraElUtc;
            Establecimientos = establecimientos;
        }
    }
}

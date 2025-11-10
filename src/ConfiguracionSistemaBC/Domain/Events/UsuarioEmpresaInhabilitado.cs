using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Domain.Events
{
    public sealed class UsuarioEmpresaInhabilitado : DomainEvent
    {
        public EmpresaId EmpresaId { get; }
        public UsuarioId UsuarioId { get; }
        public string Razon { get; }

        public UsuarioEmpresaInhabilitado(
            EmpresaId empresaId,
            UsuarioId usuarioId,
            string razon,
            DateTime? occurredOnUtc = null,
            Guid? eventId = null)
            : base(eventId, occurredOnUtc)
        {
            EmpresaId = empresaId;
            UsuarioId = usuarioId;
            Razon = razon;
        }
    }
}


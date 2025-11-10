using System;
using SharedKernel.ValueObjects;
using SharedKernel.Events;
using ConfiguracionSistemaBC.Domain.Aggregates;

namespace ConfiguracionSistemaBC.Domain.Events
{
    /// <summary>
    /// Evento de dominio que representa la creación de un UsuarioEmpresa.
    /// </summary>
    public sealed class UsuarioEmpresaCreado : DomainEvent
    {
        public UsuarioId UsuarioId { get; }
        public EmpresaId EmpresaId { get; }
        public IReadOnlyCollection<EstablecimientoId> Establecimientos { get; }
        public Email Email { get; }
        public NombrePersona Nombre { get; }
        public RolEmpresa? Rol { get; }

        public UsuarioEmpresaCreado(
            UsuarioId usuarioId,
            EmpresaId empresaId,
            IReadOnlyCollection<EstablecimientoId> establecimientos,
            Email email,
            NombrePersona nombre,
            RolEmpresa? rol,
            DateTime? occurredOnUtc = null,
            Guid? eventId = null)
            : base(eventId, occurredOnUtc)
        {
            UsuarioId = usuarioId;
            EmpresaId = empresaId;
            Establecimientos = establecimientos;
            Email = email;
            Nombre = nombre;
            Rol = rol;
        }
    }
}

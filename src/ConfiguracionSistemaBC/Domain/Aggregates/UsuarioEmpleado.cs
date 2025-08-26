using System;
using System.Collections.Generic;
using ConfiguracionSistemaBC.Domain.Interfaces;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using SharedKernel.Events;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;
using ConfiguracionSistemaBC.Domain.Events;

namespace ConfiguracionSistemaBC.Domain.Aggregates
{
    public enum EstadoUsuarioEmpleado
    {
        Inhabilitado = 0,
        Habilitado   = 1
    }

    public enum RolUsuario
    {
        Administrador = 0,
        Cajero        = 1,
        Asistente     = 2,
        Contador      = 3
        // agrega más según tu catálogo
    }

    /// <summary>
    /// Aggregate Root de "usuario adicional" configurado por el admin.
    /// El admin asigna la contraseña (hash), se genera invitación, el invitado acepta y queda Habilitado.
    /// </summary>
    public sealed class UsuarioEmpleado
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        // Identidad & concurrencia
        public Guid Id { get; private set; }
        public long Version { get; private set; } // la maneja la infra/UoW

        // Multi-tenant (obligatorios)
        public EmpresaId EmpresaId { get; private set; } = default!;
    public IReadOnlyCollection<EstablecimientoId> Establecimientos { get; private set; } = Array.Empty<EstablecimientoId>();

        // Datos (obligatorios)
    public Email Email { get; private set; } = default!;
        public NombrePersona Nombre { get; private set; } = default!;
        public RolUsuario Rol { get; private set; }

        /// <summary>Nombre de perfil personalizado (opcional, etiqueta visible para UI).</summary>
        public string? NombrePerfilPersonalizado { get; private set; }

        // Seguridad (obligatorio, nunca texto plano)
        public PasswordHash PasswordHash { get; private set; } = default!;

        // Estado e Invitación
        public EstadoUsuarioEmpleado Estado { get; private set; }
        public string? TokenInvitacion { get; private set; }
        public DateTime? TokenExpiraElUtc { get; private set; }
        public DateTime? InvitacionAceptadaElUtc { get; private set; }

        // Borrado lógico
        public DateTime? EliminadoElUtc { get; private set; }

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        private UsuarioEmpleado() { } // requerido por ORM

        private UsuarioEmpleado(
            Guid id,
            EmpresaId empresaId,
            IEnumerable<EstablecimientoId> establecimientos,
            Email email,
            NombrePersona nombre,
            RolUsuario rol,
            PasswordHash passwordHash,
            string? nombrePerfilPersonalizado)
        {
            Id = id;
            EmpresaId = empresaId ?? throw new ArgumentNullException(nameof(empresaId));
            // SucursalId eliminado: multiestablecimiento
            Email = email ?? throw new ArgumentNullException(nameof(email));
            Nombre = nombre ?? throw new ArgumentNullException(nameof(nombre));
            Rol = rol;
            PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
            NombrePerfilPersonalizado = nombrePerfilPersonalizado;

            Establecimientos = (establecimientos ?? throw new ArgumentNullException(nameof(establecimientos))).ToArray();

            Estado = EstadoUsuarioEmpleado.Inhabilitado;

            _domainEvents.Add(new UsuarioEmpleadoCreado(Id, EmpresaId, Establecimientos, Email, Nombre, Rol));
        }

        /// <summary>Fábrica DDD: valida invariantes y crea el agregado inhabilitado.</summary>
        public static UsuarioEmpleado Crear(
            EmpresaId empresaId,
            IEnumerable<EstablecimientoId> establecimientos,
            Email email,
            NombrePersona nombre,
            RolUsuario rol,
            PasswordHash passwordHash,
            string? nombrePerfilPersonalizado,
            IUnicidadUsuarioEmpleadoService unicidad)
        {
            if (unicidad is null) throw new ArgumentNullException(nameof(unicidad));
            if (!unicidad.EsEmailUnicoPorEmpresa(empresaId, email))
                throw new BusinessRuleException($"El email {email.Value} ya existe en la empresa.");
            if (establecimientos == null || !establecimientos.Any())
                throw new BusinessRuleException("Debe asignar al menos un establecimiento al usuario.");

            return new UsuarioEmpleado(Guid.NewGuid(), empresaId, establecimientos, email, nombre, rol, passwordHash, nombrePerfilPersonalizado);
        }

        /// <summary>Genera invitación (token/expiración) y emite evento para enviar correo.</summary>
    public void GenerarInvitacion(string token, DateTime expiraElUtc, DateTime ahoraUtc)
        {
            EnsureNoEliminado();
            if (Estado == EstadoUsuarioEmpleado.Habilitado)
                throw new BusinessRuleException("El usuario ya está habilitado.");
            if (string.IsNullOrWhiteSpace(token))
                throw new BusinessRuleException("Token de invitación inválido.");
            if (expiraElUtc <= ahoraUtc)
                throw new BusinessRuleException("La invitación debe expirar en el futuro.");

            TokenInvitacion = token;
            TokenExpiraElUtc = expiraElUtc;
            InvitacionAceptadaElUtc = null;

            _domainEvents.Add(new InvitacionUsuarioEmpleadoEnviada(Id, EmpresaId, Email, token, expiraElUtc));
        }

        /// <summary>Acepta la invitación (token válido y no expirado) → Habilitado.</summary>
        public void AceptarInvitacion(string tokenIngresado, DateTime ahoraUtc)
        {
            EnsureNoEliminado();
            if (Estado == EstadoUsuarioEmpleado.Habilitado)
                throw new BusinessRuleException("El usuario ya está habilitado.");
            if (string.IsNullOrWhiteSpace(TokenInvitacion) || !TokenExpiraElUtc.HasValue)
                throw new BusinessRuleException("No hay invitación vigente.");
            if (!string.Equals(TokenInvitacion, tokenIngresado, StringComparison.Ordinal))
                throw new BusinessRuleException("Token de invitación no válido.");
            if (ahoraUtc > TokenExpiraElUtc.Value)
                throw new BusinessRuleException("La invitación ha expirado.");

            InvitacionAceptadaElUtc = ahoraUtc;

            // (Recomendado) limpiar token tras aceptar
            TokenInvitacion = null;
            TokenExpiraElUtc = null;

            Estado = EstadoUsuarioEmpleado.Habilitado;

            _domainEvents.Add(new InvitacionUsuarioEmpleadoAceptada(Id, EmpresaId, Email));
            // Integración con el BC de Identidad: provisionar credenciales con el hash ya asignado
            _domainEvents.Add(new SolicitarProvisionEnIdentidad(Id, EmpresaId, Email, PasswordHash, Rol));
        }

        public void Inhabilitar(string razon)
        {
            EnsureNoEliminado();
            if (Estado == EstadoUsuarioEmpleado.Inhabilitado) return;
            Estado = EstadoUsuarioEmpleado.Inhabilitado;
            _domainEvents.Add(new UsuarioEmpleadoInhabilitado(Id, EmpresaId, razon));
        }

        public void Habilitar()
        {
            EnsureNoEliminado();
            if (Estado == EstadoUsuarioEmpleado.Habilitado) return;

            // Si quieres forzar el flujo por invitación, conserva esta regla:
            if (!InvitacionAceptadaElUtc.HasValue)
                throw new BusinessRuleException("Para habilitar, el usuario debe aceptar la invitación.");

            Estado = EstadoUsuarioEmpleado.Habilitado;
            _domainEvents.Add(new UsuarioEmpleadoHabilitado(Id, EmpresaId));
        }

        public void ActualizarPassword(PasswordHash nuevoHash)
        {
            EnsureNoEliminado();
            if (Estado != EstadoUsuarioEmpleado.Habilitado)
                throw new BusinessRuleException("Solo un usuario habilitado puede actualizar su contraseña.");
            PasswordHash = nuevoHash ?? throw new ArgumentNullException(nameof(nuevoHash));
            _domainEvents.Add(new PasswordDeUsuarioEmpleadoActualizada(Id, EmpresaId));
        }

        public void ActualizarRol(RolUsuario nuevoRol)
        {
            EnsureNoEliminado();
            if (Rol == nuevoRol) return;
            Rol = nuevoRol;
            _domainEvents.Add(new RolDeUsuarioEmpleadoActualizado(Id, EmpresaId, Rol));
        }

        /// <summary>
        /// Eliminación por error administrativo.
        /// Solo si está INHABILITADO y sin actividad registrada.
        /// </summary>
        public void EliminarPorErrorAdministrativo(string razon, IUsuarioEmpleadoActividadService actividadService, DateTime ahoraUtc)
        {
            if (Estado != EstadoUsuarioEmpleado.Inhabilitado)
                throw new BusinessRuleException("Solo se puede eliminar un usuario inhabilitado por error administrativo.");
            if (actividadService is null)
                throw new ArgumentNullException(nameof(actividadService));
            if (actividadService.TieneAcciones(Id))
                throw new BusinessRuleException("No se puede eliminar: el usuario ya realizó acciones en el sistema.");

            EliminadoElUtc = ahoraUtc;
            _domainEvents.Add(new UsuarioEmpleadoEliminado(Id, EmpresaId, razon));
        }

        public void ClearDomainEvents() => _domainEvents.Clear();

        // ===== Eventos de dominio (lenguaje del dominio, sin detalles de infraestructura) =====
        public sealed record UsuarioEmpleadoCreado(
            Guid UsuarioEmpleadoId, EmpresaId EmpresaId, IReadOnlyCollection<EstablecimientoId> Establecimientos,
            Email Email, NombrePersona Nombre, RolUsuario Rol) : IDomainEvent;

        public sealed record UsuarioEmpleadoEliminado(
            Guid UsuarioEmpleadoId, EmpresaId EmpresaId, string Razon) : IDomainEvent;

        public sealed record InvitacionUsuarioEmpleadoEnviada(
            Guid UsuarioEmpleadoId, EmpresaId EmpresaId, Email Email,
            string Token, DateTime ExpiraElUtc) : IDomainEvent;

        public sealed record InvitacionUsuarioEmpleadoAceptada(
            Guid UsuarioEmpleadoId, EmpresaId EmpresaId, Email Email) : IDomainEvent;

        public sealed record SolicitarProvisionEnIdentidad(
            Guid UsuarioEmpleadoId, EmpresaId EmpresaId, Email Email,
            PasswordHash PasswordHash, RolUsuario Rol) : IDomainEvent;

        public sealed record UsuarioEmpleadoHabilitado(
            Guid UsuarioEmpleadoId, EmpresaId EmpresaId) : IDomainEvent;

        public sealed record UsuarioEmpleadoInhabilitado(
            Guid UsuarioEmpleadoId, EmpresaId EmpresaId, string Razon) : IDomainEvent;

        public sealed record PasswordDeUsuarioEmpleadoActualizada(
            Guid UsuarioEmpleadoId, EmpresaId EmpresaId) : IDomainEvent;

        public sealed record RolDeUsuarioEmpleadoActualizado(
            Guid UsuarioEmpleadoId, EmpresaId EmpresaId, RolUsuario Rol) : IDomainEvent;

        // ===== Helpers de invariante =====
        private void EnsureNoEliminado()
        {
            if (EliminadoElUtc.HasValue)
                throw new BusinessRuleException("El usuario fue eliminado.");
        }
    }
}

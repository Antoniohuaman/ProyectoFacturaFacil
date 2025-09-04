using System;
using System.Collections.Generic;
using System.Linq;
using SharedKernel.Exceptions;
using ConfiguracionSistemaBC.Domain.Events;
using SharedKernel.Events;                    // IDomainEvent
using SharedKernel.ValueObjects;             // EmpresaId, UsuarioId, EstablecimientoId, DocumentoIdentidad, NombrePersona, Email, Telefono

namespace ConfiguracionSistemaBC.Domain.Aggregates
{
    public enum UsuarioEmpresaEstado { Invitado = 0, Habilitado = 1, Inhabilitado = 2 }

    /// <summary>
    /// Membresía de un usuario global (UsuarioId) dentro de una empresa.
    /// Administra datos de contacto, roles (empresa y por establecimiento) y estado.
    /// </summary>
    public sealed class UsuarioEmpresa
    {
    /// <summary>
        private readonly List<IDomainEvent> _domainEvents = new();

        // Identidad compuesta (tenant + user)
        public EmpresaId EmpresaId { get; }
        public UsuarioId UsuarioId { get; }

        // Datos de trabajo (no credenciales)
        public DocumentoIdentidad? Documento { get; private set; }     // DNI/RUC opcional en tu UX
        public NombrePersona Nombre { get; private set; }
        public Email EmailContacto { get; private set; }
        public Telefono? TelefonoContacto { get; private set; }

        // Estado / concurrencia
        public UsuarioEmpresaEstado Estado { get; private set; }
        public bool EstaHabilitado => Estado == UsuarioEmpresaEstado.Habilitado;
        public int Version { get; private set; }

        // Roles de ámbito EMPRESA (aplican a todos los establecimientos)
        private readonly HashSet<Guid> _rolesEmpresaIds = new();
    public IReadOnlyCollection<Guid> RolesEmpresaIds => _rolesEmpresaIds.ToList().AsReadOnly();

        // Accesos por establecimiento
        private readonly List<AccesoEstablecimiento> _accesos = new();
        public IReadOnlyCollection<AccesoEstablecimiento> Accesos => _accesos.AsReadOnly();

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        /// <summary>
        /// Indica si el usuario puede ser eliminado físicamente del sistema.
        /// Solo es posible si el usuario no ha realizado acciones relevantes (por ejemplo, no ha operado, registrado movimientos, etc.).
        /// </summary>
        public bool PuedeSerEliminado => !HaRealizadoAccionesRelevantes;

        // Esta propiedad debe actualizarse cuando el usuario realiza acciones relevantes en la empresa.
        private bool HaRealizadoAccionesRelevantes = false;

        /// <summary>
        /// Marcar que el usuario realizó una acción relevante (por ejemplo, operación, registro, etc.).
        /// Llamar este método desde los puntos del dominio donde el usuario interactúa de forma significativa.
        /// </summary>
        public void MarcarAccionRelevante()
        {
            HaRealizadoAccionesRelevantes = true;
            Version++;
        }

        // ------------------------ Ctor ------------------------
        private UsuarioEmpresa(
            EmpresaId empresaId,
            UsuarioId usuarioId,
            DocumentoIdentidad? documento,
            NombrePersona nombre,
            Email emailContacto,
            Telefono? telefonoContacto)
        {
            EmpresaId = empresaId ?? throw new ArgumentNullException(nameof(empresaId));
            UsuarioId = usuarioId ?? throw new ArgumentNullException(nameof(usuarioId));
            Documento = documento; // opcional
            Nombre = nombre ?? throw new ArgumentNullException(nameof(nombre));
            EmailContacto = emailContacto ?? throw new ArgumentNullException(nameof(emailContacto));
            TelefonoContacto = telefonoContacto;

            Estado = UsuarioEmpresaEstado.Invitado;
            Version = 0;

            _domainEvents.Add(new ConfiguracionSistemaBC.Domain.Events.UsuarioEmpresaCreado(
                UsuarioId,
                EmpresaId,
                new List<EstablecimientoId>(),
                EmailContacto,
                Nombre,
                null // RolEmpresa, si aplica
            ));
        }

        /// <summary>Fábrica DDD: crea la membresía en estado Invitado y asigna roles/accesos opcionales.</summary>
        public static UsuarioEmpresa Crear(
            EmpresaId empresaId,
            UsuarioId usuarioId,
            DocumentoIdentidad? documento,
            NombrePersona nombre,
            Email emailContacto,
            Telefono? telefonoContacto,
            IEnumerable<Guid>? rolesEmpresaIds = null,
            IEnumerable<(EstablecimientoId EstablecimientoId, IEnumerable<Guid> RolIds)>? accesosIniciales = null)
        {
            var agg = new UsuarioEmpresa(empresaId, usuarioId, documento, nombre, emailContacto, telefonoContacto);

            if (rolesEmpresaIds is not null) agg.AsignarRolesEmpresa(rolesEmpresaIds);
            if (accesosIniciales is not null) agg.ReemplazarAccesos(accesosIniciales);

            return agg;
        }

        // ------------------------ Comportamiento ------------------------

        public void ActualizarDocumento(DocumentoIdentidad? documento)
        {
            Documento = documento; // puede ser null
            Version++;
            // Evento de actualización de datos, si existe en Domain.Events, usarlo aquí
        }

        public void ActualizarDatosContacto(NombrePersona nombre, Email email, Telefono? telefono)
        {
            Nombre = nombre ?? throw new ArgumentNullException(nameof(nombre));
            EmailContacto = email ?? throw new ArgumentNullException(nameof(email));
            TelefonoContacto = telefono;
            Version++;
            // Evento de actualización de datos, si existe en Domain.Events, usarlo aquí
        }

        /// <summary>Reemplaza el set de roles de empresa (deduplica, ignora Guid.Empty).</summary>
        public void AsignarRolesEmpresa(IEnumerable<Guid> rolIdsEmpresa)
        {
            if (rolIdsEmpresa is null) throw new ArgumentNullException(nameof(rolIdsEmpresa));

            _rolesEmpresaIds.Clear();
            foreach (var id in rolIdsEmpresa.Where(x => x != Guid.Empty).Distinct())
                _rolesEmpresaIds.Add(id);

            Version++;
            // Evento de asignación de roles, si existe en Domain.Events, usarlo aquí
        }

        /// <summary>Agrega un rol de empresa sin reemplazar los existentes.</summary>
        public void AgregarRolEmpresa(Guid rolId)
        {
            if (rolId == Guid.Empty) throw new BusinessRuleException("RolId inválido.");
            if (_rolesEmpresaIds.Add(rolId))
            {
                Version++;
                // Evento de asignación de roles, si existe en Domain.Events, usarlo aquí
            }
        }

        /// <summary>Quita un rol de empresa.</summary>
        public void QuitarRolEmpresa(Guid rolId)
        {
            if (_rolesEmpresaIds.Remove(rolId))
            {
                Version++;
                // Evento de asignación de roles, si existe en Domain.Events, usarlo aquí
            }
        }

        /// <summary>Agrega o reemplaza roles para un establecimiento concreto.</summary>
        public void AsignarRolesAlEstablecimiento(EstablecimientoId estId, IEnumerable<Guid> rolIdsEst)
        {
            var acceso = _accesos.FirstOrDefault(a => a.EstablecimientoId == estId);
            if (acceso is null)
                _accesos.Add(new AccesoEstablecimiento(estId, rolIdsEst));
            else
                acceso.ReemplazarRoles(rolIdsEst);

            Version++;
            // Evento de actualización de accesos, si existe en Domain.Events, usarlo aquí
        }

        /// <summary>Agrega (merge) roles a un establecimiento SIN reemplazar los existentes.</summary>
        public void AgregarRolesAlEstablecimiento(EstablecimientoId estId, IEnumerable<Guid> rolIdsEst)
        {
            var acceso = _accesos.FirstOrDefault(a => a.EstablecimientoId == estId);
            if (acceso is null)
                _accesos.Add(new AccesoEstablecimiento(estId, rolIdsEst));
            else
                acceso.AgregarRoles(rolIdsEst);

            Version++;
            // Evento de actualización de accesos, si existe en Domain.Events, usarlo aquí
        }

        /// <summary>Quita un rol puntual de un establecimiento.</summary>
        public void QuitarRolDeEstablecimiento(EstablecimientoId estId, Guid rolId)
        {
            var acceso = _accesos.FirstOrDefault(a => a.EstablecimientoId == estId);
            if (acceso is null) return;

            if (acceso.QuitarRol(rolId))
            {
                Version++;
                // Evento de actualización de accesos, si existe en Domain.Events, usarlo aquí
            }
        }

        /// <summary>Reemplaza completamente la lista de accesos (uno por establecimiento).</summary>
        public void ReemplazarAccesos(IEnumerable<(EstablecimientoId EstablecimientoId, IEnumerable<Guid> RolIds)> accesos)
        {
            if (accesos is null) throw new ArgumentNullException(nameof(accesos));

            var nuevos = accesos.Select(a => new AccesoEstablecimiento(a.EstablecimientoId, a.RolIds)).ToList();

            // No duplicados por establecimiento
            var duplicados = nuevos.GroupBy(x => x.EstablecimientoId).Any(g => g.Count() > 1);
            if (duplicados) throw new BusinessRuleException("No puede repetir un mismo establecimiento en los accesos.");

            _accesos.Clear();
            _accesos.AddRange(nuevos);
            Version++;
            // Evento de actualización de accesos, si existe en Domain.Events, usarlo aquí
        }

        /// <summary>Asigna un rol a TODOS los establecimientos indicados. Si reemplazar=true, deja solo ese rol en cada uno.</summary>
    /// <remarks>
    /// Si el usuario no tiene acceso previo a un establecimiento, se crea el acceso y se asigna el rol indicado.
    /// Si ya tiene acceso:
    /// - Si reemplazar=true, se eliminan los roles previos y se deja solo el nuevo rol.
    /// - Si reemplazar=false, se agrega el nuevo rol a los existentes.
    /// </remarks>
        public void AsignarRolATodosLosEstablecimientos(IEnumerable<EstablecimientoId> establecimientos, Guid rolId, bool reemplazar = false)
        {
            if (establecimientos is null) throw new ArgumentNullException(nameof(establecimientos));
            if (rolId == Guid.Empty) throw new BusinessRuleException("RolId inválido.");

            var any = false;
            foreach (var est in establecimientos.Distinct())
            {
                var acc = _accesos.FirstOrDefault(a => a.EstablecimientoId == est);
                if (acc is null)
                {
                    _accesos.Add(new AccesoEstablecimiento(est, new[] { rolId }));
                    any = true;
                }
                else
                {
                    if (reemplazar)
                        acc.ReemplazarRoles(new[] { rolId });
                    else
                        acc.AgregarRoles(new[] { rolId });
                    any = true;
                }
            }

            if (any)
            {
                Version++;
                // Evento de actualización de accesos, si existe en Domain.Events, usarlo aquí
            }
        }

        public void RemoverAcceso(EstablecimientoId estId)
        {
            var removed = _accesos.RemoveAll(a => a.EstablecimientoId == estId);
            if (removed > 0)
            {
                Version++;
                // Evento de actualización de accesos, si existe en Domain.Events, usarlo aquí
            }
        }

        /// <summary>Se llama cuando Identidad confirma la cuenta del usuario.</summary>
        public void MarcarConfirmadoPorIdentidad()
        {
            if (Estado == UsuarioEmpresaEstado.Habilitado) return;
            Estado = UsuarioEmpresaEstado.Habilitado;
            Version++;
            _domainEvents.Add(new UsuarioEmpresaHabilitado(
                EmpresaId,
                _accesos.Select(a => a.EstablecimientoId).ToList()
            ));
        }

        public void Inhabilitar(string razon)
        {
            if (Estado == UsuarioEmpresaEstado.Inhabilitado) return;
            Estado = UsuarioEmpresaEstado.Inhabilitado;
            Version++;
            _domainEvents.Add(new ConfiguracionSistemaBC.Domain.Events.UsuarioEmpresaInhabilitado(
                EmpresaId,
                UsuarioId,
                razon
            ));
        }

        /// <summary>Unión de roles de empresa con los del establecimiento indicado.</summary>
        public IReadOnlyCollection<Guid> ObtenerRolesEfectivos(EstablecimientoId estId)
        {
            var roles = new HashSet<Guid>(_rolesEmpresaIds);
            var local = _accesos.FirstOrDefault(a => a.EstablecimientoId == estId);
            if (local is not null) foreach (var r in local.RolIds) roles.Add(r);
            return roles.ToList().AsReadOnly();
        }

        public void ClearDomainEvents() => _domainEvents.Clear();

        /// <summary>Útil en tests para fijar versión.</summary>
        public void ForzarVersionParaPruebas(int version) => Version = version;
    }

    /// <summary>Componente del aggregate: acceso del usuario a un establecimiento con sus roles locales.</summary>
    public sealed class AccesoEstablecimiento
    {
        public EstablecimientoId EstablecimientoId { get; }
        private readonly HashSet<Guid> _rolIds = new();
    public IReadOnlyCollection<Guid> RolIds => _rolIds.ToList().AsReadOnly();

        public AccesoEstablecimiento(EstablecimientoId estId, IEnumerable<Guid> rolIds)
        {
            if (estId is null) throw new BusinessRuleException("EstablecimientoId es obligatorio.");
            EstablecimientoId = estId;
            ReemplazarRoles(rolIds);
        }

        public void ReemplazarRoles(IEnumerable<Guid> rolIds)
        {
            if (rolIds is null) throw new ArgumentNullException(nameof(rolIds));
            var lista = rolIds.Where(id => id != Guid.Empty).Distinct().ToArray();
            if (lista.Length == 0)
                throw new BusinessRuleException("Debe asignar al menos un rol para el establecimiento.");

            _rolIds.Clear();
            foreach (var id in lista) _rolIds.Add(id);
        }

        public void AgregarRoles(IEnumerable<Guid> rolIds)
        {
            if (rolIds is null) throw new ArgumentNullException(nameof(rolIds));
            var lista = rolIds.Where(id => id != Guid.Empty).Distinct();
            foreach (var id in lista) _rolIds.Add(id);
        }

        public bool QuitarRol(Guid rolId) => _rolIds.Remove(rolId);
    }

  
}

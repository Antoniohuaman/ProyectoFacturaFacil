using System;
using System.Collections.Generic;
using System.Linq;
using SharedKernel.Exceptions;
using SharedKernel.Events;               // IDomainEvent
using SharedKernel.ValueObjects;         // EmpresaId
using ConfiguracionSistemaBC.Domain.ValueObjects; // Permiso, Recurso, Accion

namespace ConfiguracionSistemaBC.Domain.Aggregates
{
    /// <summary>
    /// AGGREGATE ROOT: Rol reusable para múltiples usuarios.
    /// - Rol de sistema: EmpresaId == null (inmutable; catálogo base).
    /// - Rol personalizado: EmpresaId != null (editable por esa empresa).
    /// El alcance (todos los establecimientos o un subconjunto) se resuelve en UsuarioEmpresa.
    /// </summary>
    public sealed class RolEmpresa
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        public Guid RolId { get; }
        public EmpresaId? EmpresaId { get; } // null => rol de sistema
        public string Nombre { get; private set; }
        public bool EsSistema => EmpresaId is null;
        public int Version { get; private set; }

        private readonly List<Permiso> _permisos = new();
        public IReadOnlyCollection<Permiso> Permisos => _permisos.AsReadOnly();

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        // ---------- Fábricas ----------
        private RolEmpresa(Guid id, EmpresaId? empresaId, string nombre, IEnumerable<Permiso> permisos)
        {
            RolId = id;
            EmpresaId = empresaId; // null => sistema
            Nombre = ValidarNombre(nombre);
            EstablecerPermisosInterno(permisos);  // consolida
            Version = 1;

            _domainEvents.Add(new RolEmpresaCreado(RolId, EmpresaId, Nombre));
        }

        public static RolEmpresa CrearSistema(string nombre, IEnumerable<Permiso> permisos) =>
            new(Guid.NewGuid(), null, nombre, permisos);

        public static RolEmpresa CrearPersonalizado(EmpresaId empresaId, string nombre, IEnumerable<Permiso> permisos)
        {
            if (empresaId is null) throw new BusinessRuleException("EmpresaId es obligatorio.");
            return new RolEmpresa(Guid.NewGuid(), empresaId, nombre, permisos);
        }

        /// <summary>Clona un rol de sistema como rol personalizado para una empresa.</summary>
        public RolEmpresa ClonarParaEmpresa(EmpresaId empresaId, string? nuevoNombre = null)
        {
            if (!EsSistema) throw new BusinessRuleException("Solo un rol de sistema puede clonarse.");
            return CrearPersonalizado(empresaId, nuevoNombre ?? Nombre, _permisos);
        }

        // ---------- Comportamiento ----------
        public void Renombrar(string nombre)
        {
            if (EsSistema)
                throw new BusinessRuleException("No se puede renombrar un rol de sistema. Clona para personalizar.");
            Nombre = ValidarNombre(nombre);
            Version++;
            _domainEvents.Add(new RolEmpresaRenombrado(RolId, EmpresaId!, Nombre));
        }

        /// <summary>Reemplaza el set de permisos; consolida por recurso (OR de Accion). Solo personalizado.</summary>
        public void ReemplazarPermisos(IEnumerable<Permiso> permisos)
        {
            if (EsSistema)
                throw new BusinessRuleException("No se pueden modificar permisos de un rol de sistema. Clona para personalizar.");
            EstablecerPermisosInterno(permisos);
            Version++;
            _domainEvents.Add(new PermisosDeRolEmpresaActualizados(RolId, EmpresaId));
        }

        public bool TienePermiso(Recurso recurso, Accion accion)
        {
            var p = _permisos.FirstOrDefault(x => x.Recurso == recurso);
            return p is not null && p.Contiene(accion);
        }

        public void ClearDomainEvents() => _domainEvents.Clear();

        // ---------- Internos ----------
        private void EstablecerPermisosInterno(IEnumerable<Permiso> permisos)
        {
            if (permisos is null) throw new ArgumentNullException(nameof(permisos));
            var list = permisos.ToList();
            if (list.Count == 0) throw new BusinessRuleException("El rol debe tener al menos un permiso.");

            var consolidados = list
                .GroupBy(p => p.Recurso)
                .Select(g => new Permiso(g.Key,
                    g.Select(x => x.Acciones).Aggregate(Accion.Ninguna, (a, b) => a | b)))
                .ToList();

            _permisos.Clear();
            _permisos.AddRange(consolidados);
        }

        private static string ValidarNombre(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                throw new BusinessRuleException("El nombre del rol es obligatorio.");
            var v = valor.Trim();
            if (v.Length > 60)
                throw new BusinessRuleException("El nombre del rol excede 60 caracteres.");
            return v;
        }

        // ---------- Catálogo base (sistema, inmutables) ----------
        public static class CatalogoSistema
        {
            public static RolEmpresa Administrador()
                => CrearSistema("Administrador", new[]
                {
                    new Permiso(Recurso.Configuracion, Accion.Ver | Accion.Configurar | Accion.Exportar),
                    new Permiso(Recurso.Usuarios, Accion.Ver | Accion.Crear | Accion.Editar | Accion.Eliminar | Accion.Configurar),
                    new Permiso(Recurso.Establecimientos, Accion.Ver | Accion.Crear | Accion.Editar | Accion.Eliminar | Accion.Configurar),
                    new Permiso(Recurso.Comprobantes, Accion.Ver | Accion.Emitir | Accion.Anular | Accion.Exportar | Accion.Configurar),
                    new Permiso(Recurso.Caja, Accion.Ver | Accion.Crear | Accion.Editar | Accion.Eliminar | Accion.Exportar),
                    new Permiso(Recurso.Indicadores, Accion.Ver | Accion.Exportar)
                });

            public static RolEmpresa Cajero()
                => CrearSistema("Cajero", new[]
                {
                    new Permiso(Recurso.Comprobantes, Accion.Ver | Accion.Emitir | Accion.Anular),
                    new Permiso(Recurso.Caja, Accion.Ver | Accion.Crear | Accion.Editar | Accion.Eliminar)
                });

            public static RolEmpresa Vendedor()
                => CrearSistema("Vendedor", new[]
                {
                    new Permiso(Recurso.Comprobantes, Accion.Ver | Accion.Emitir),
                    new Permiso(Recurso.Clientes, Accion.Ver | Accion.Crear | Accion.Editar)
                });

            public static RolEmpresa Contador()
                => CrearSistema("Contador", new[]
                {
                    new Permiso(Recurso.Comprobantes, Accion.Ver | Accion.Exportar | Accion.Aprobar),
                    new Permiso(Recurso.Configuracion, Accion.Ver | Accion.Exportar)
                });

            public static RolEmpresa Asistente()
                => CrearSistema("Asistente", new[]
                {
                    new Permiso(Recurso.Clientes, Accion.Ver | Accion.Crear | Accion.Editar),
                    new Permiso(Recurso.Articulos, Accion.Ver | Accion.Crear | Accion.Editar)
                });
        }
    }

    // ---------- Eventos ----------
    public sealed record RolEmpresaCreado(Guid RolId, EmpresaId? EmpresaId, string Nombre) : IDomainEvent;
    public sealed record RolEmpresaRenombrado(Guid RolId, EmpresaId EmpresaId, string Nombre) : IDomainEvent;
    public sealed record PermisosDeRolEmpresaActualizados(Guid RolId, EmpresaId? EmpresaId) : IDomainEvent;
}

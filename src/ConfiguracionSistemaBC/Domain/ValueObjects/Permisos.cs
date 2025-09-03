using System;

namespace ConfiguracionSistemaBC.Domain.ValueObjects
{
    /// <summary>
    /// Acciones disponibles sobre un recurso. Se usa como flags (bitwise).
    /// </summary>
    [Flags]
    public enum Accion
    {
        Ninguna    = 0,
        Ver        = 1 << 0,
        Crear      = 1 << 1,
        Editar     = 1 << 2,
        Eliminar   = 1 << 3,
        Emitir     = 1 << 4,
        Anular     = 1 << 5,
        Aprobar    = 1 << 6,
        Exportar   = 1 << 7,
        Configurar = 1 << 8
    }

    /// <summary>
    /// Recursos/módulos a los que se aplican permisos.
    /// </summary>
    public enum Recurso
    {
        Usuarios,
        Establecimientos,
        Comprobantes,
        Caja,
        Clientes,
        Articulos,
        Precios,
        Indicadores,
        Configuracion
    }

    /// <summary>
    /// VO que expresa las acciones permitidas sobre un recurso.
    /// </summary>
    public sealed record Permiso(Recurso Recurso, Accion Acciones)
    {
        /// <summary>Retorna true si el permiso contiene la acción indicada.</summary>
        public bool Contiene(Accion accion) => (Acciones & accion) == accion;

        /// <summary>Conveniencia: solo lectura.</summary>
        public static Permiso SoloLeer(Recurso r) =>
            new(r, Accion.Ver);

        /// <summary>Conveniencia: operaciones básicas de mantenimiento.</summary>
        public static Permiso CRUD(Recurso r) =>
            new(r, Accion.Ver | Accion.Crear | Accion.Editar | Accion.Eliminar);
    }
}

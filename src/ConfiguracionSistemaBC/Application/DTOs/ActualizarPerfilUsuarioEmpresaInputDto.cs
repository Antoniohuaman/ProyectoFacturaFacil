using System;
using System.Collections.Generic;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Datos opcionales para actualizar el perfil de un usuario dentro de la empresa actual.
    /// El email NO se puede cambiar.
    /// </summary>
    public sealed class ActualizarPerfilUsuarioEmpresaInputDto
    {
        /// <summary>Identidad del usuario a actualizar (Guid del usuario global).</summary>
        public Guid UsuarioId { get; init; }

        /// <summary>Versión esperada para concurrencia optimista.</summary>
        public int ExpectedVersion { get; init; }

        /// <summary>Nuevos nombres (opcional). Si se omite, se conserva.</summary>
        public string? Nombres { get; init; }

        /// <summary>Nuevos apellidos (opcional). Si se omite, se conserva.</summary>
        public string? Apellidos { get; init; }

        /// <summary>
        /// Nuevo teléfono (opcional).
        /// - Si es null: no se modifica.
        /// - Si es cadena vacía o solo espacios: se borra (queda sin teléfono).
        /// - Si trae valor, se valida y asigna.
        /// </summary>
        public string? Telefono { get; init; }

        /// <summary>
        /// Reemplazo completo de roles de EMPRESA (opcional).
        /// Si se provee, se valida y se reemplaza el set; si es null, no cambia.
        /// </summary>
        public List<Guid>? RolesEmpresaIds { get; init; }

        /// <summary>
        /// Reemplazo completo de accesos por establecimiento (opcional).
        /// Si se provee, se valida y se reemplaza; si es null, no cambia.
        /// </summary>
        public List<AccesoIn>? AccesosPorEstablecimiento { get; init; }

        public sealed class AccesoIn
        {
            public Guid EstablecimientoId { get; init; }
            public List<Guid> RolIds { get; init; } = new();
        }
    }
}

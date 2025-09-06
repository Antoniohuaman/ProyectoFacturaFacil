using System;
using System.Collections.Generic;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Datos de entrada para registrar a un usuario dentro de la empresa actual (multiempresa).
    /// </summary>
    public sealed class RegistrarUsuarioEmpresaInputDto
    {
        // Identidad / contacto básicos
        public string Nombres { get; init; } = string.Empty;
        public string Apellidos { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string? Telefono { get; init; }

        /// <summary>
        /// (Opcional) Roles de ámbito EMPRESA (aplican a todos los establecimientos).
        /// Se validan como roles de sistema o roles personalizados de ESTA empresa.
        /// </summary>
        public List<Guid>? RolesEmpresaIds { get; init; }

        /// <summary>
        /// Accesos por establecimiento (obligatorio al menos uno). Cada acceso debe tener ≥1 rol.
        /// </summary>
        public List<AccesoIn>? AccesosPorEstablecimiento { get; init; }

        public sealed class AccesoIn
        {
            public Guid EstablecimientoId { get; init; }
            public List<Guid> RolIds { get; init; } = new();
        }
    }
}

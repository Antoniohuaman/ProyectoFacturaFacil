using System;
using System.Collections.Generic;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Resumen del usuario luego de la actualización.
    /// </summary>
    public sealed class ActualizarPerfilUsuarioEmpresaOutputDto
    {
        public Guid UsuarioId { get; init; }
        public string Estado { get; init; } = string.Empty;

        public string NombreCompleto { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Telefono { get; init; } = string.Empty;

        public List<Guid> RolesEmpresaIds { get; init; } = new();
        public List<AccesoOut> Accesos { get; init; } = new();

        public sealed class AccesoOut
        {
            public Guid EstablecimientoId { get; init; }
            public List<Guid> RolIds { get; init; } = new();
        }
    }
}

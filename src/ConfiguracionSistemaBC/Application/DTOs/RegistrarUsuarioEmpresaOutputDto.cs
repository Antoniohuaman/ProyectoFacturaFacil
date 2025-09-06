using System;
using System.Collections.Generic;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Resultado del registro de usuario en la empresa.
    /// </summary>
    public sealed class RegistrarUsuarioEmpresaOutputDto
    {
        public Guid UsuarioId { get; init; }
        public string Estado { get; init; } = string.Empty;
        public string NombreCompleto { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Telefono { get; init; } = string.Empty;

        public List<AccesoOut> Accesos { get; init; } = new();
        public List<Guid> RolesEmpresaIds { get; init; } = new();

        public sealed class AccesoOut
        {
            public Guid EstablecimientoId { get; init; }
            public Guid[] RolIds { get; init; } = Array.Empty<Guid>();
        }
    }
}

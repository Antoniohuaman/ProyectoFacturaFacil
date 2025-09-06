
using System;
using System.Collections.Generic;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Resumen de la membresía creada en la empresa (estado, accesos y roles).
    /// </summary>
    public sealed class RegistrarUsuarioEmpresaOutputDto
    {
        public Guid EmpresaId { get; init; }
        public Guid UsuarioId { get; init; }
        public string Estado { get; init; } = "Invitado";
    public string Nombres { get; init; } = string.Empty;
    public string Apellidos { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
    public string? Telefono { get; init; }
    // Documento eliminado según requerimiento

        public List<AccesoOut> Accesos { get; init; } = new();
        public List<Guid> RolesEmpresaIds { get; init; } = new();

        public sealed class AccesoOut
        {
            public Guid EstablecimientoId { get; init; }
            public Guid[] RolIds { get; init; } = Array.Empty<Guid>();
        }
    }
}

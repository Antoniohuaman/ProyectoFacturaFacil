using System;
using System.Collections.Generic;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Datos para registrar a un usuario (membrete) dentro de una empresa.
    /// La creación de cuenta/autenticación vive en otro BC (Identidad).
    /// </summary>
    public sealed class RegistrarUsuarioEmpresaInputDto
    {

    // El RUC (EmpresaId) siempre se toma del contexto de tenant, nunca del input.

        // ---- Datos personales / contacto (obligatorio lo marcado) ----
    public string Nombres { get; init; } = string.Empty; // obligatorio
    public string Apellidos { get; init; } = string.Empty; // obligatorio
    public string Email { get; init; } = string.Empty;          // obligatorio
    public string? Telefono { get; init; }

    // Documento eliminado según requerimiento

        /// <summary>
        /// Roles de ámbito empresa (se aplican a todos los establecimientos del usuario).
        /// Pueden ser roles del sistema (EmpresaId == null) o roles personalizados de esta empresa.
        /// </summary>
    public List<Guid>? RolesEmpresaIds { get; init; }

        /// <summary>
        /// Accesos por establecimiento con roles locales.
        /// Debe incluir al menos un establecimiento con al menos un rol asignado.
        /// </summary>
    public List<AccesoEstablecimientoDto> AccesosPorEstablecimiento { get; init; } = new();

        public sealed class AccesoEstablecimientoDto
        {
            public Guid EstablecimientoId { get; init; }
            public List<Guid> RolIds { get; init; } = new();
        }
    }
}

using System;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Filtros y paginación para consultar usuarios de la empresa actual.
    /// </summary>
    public sealed class ConsultarUsuariosEmpresaInputDto
    {
        /// <summary>
        /// Estado del usuario: "INVITADO" | "HABILITADO" | "INHABILITADO".
        /// Opcional. Si no se envía, no se filtra por estado.
        /// </summary>
        public string? Estado { get; init; }

        /// <summary>Filtrar por un establecimiento específico (opcional).</summary>
        public Guid? EstablecimientoId { get; init; }

        /// <summary>Filtrar por un rol específico (opcional).</summary>
        public Guid? RolId { get; init; }

        /// <summary>Número de página (>= 1). Por defecto 1.</summary>
        public int Page { get; init; } = 1;

        /// <summary>Tamaño de página (1..200). Por defecto 50.</summary>
        public int PageSize { get; init; } = 50;
    }
}

using System;
using System.Collections.Generic;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Resultado paginado de usuarios de empresa.
    /// </summary>
    public sealed class ConsultarUsuariosEmpresaOutputDto
    {
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int ItemsCount { get; init; }

        /// <summary>
        /// Total de registros que cumplen los filtros.
        /// Solo se informa cuando NO hay filtro por Establecimiento ni Rol,
        /// porque el repositorio expone CountAsync únicamente por estado.
        /// Si hay filtros por Establecimiento/Rol, será null.
        /// </summary>
        public int? Total { get; init; }

        public List<UsuarioEmpresaItem> Items { get; init; } = new();

        public sealed class UsuarioEmpresaItem
        {
            public Guid UsuarioId { get; init; }
            public string Estado { get; init; } = string.Empty;

            public string NombreCompleto { get; init; } = string.Empty;
            public string Email { get; init; } = string.Empty;
            public string Telefono { get; init; } = string.Empty;

            public List<Guid> RolesEmpresaIds { get; init; } = new();
            public List<AccesoItem> Accesos { get; init; } = new();
        }

        public sealed class AccesoItem
        {
            public Guid EstablecimientoId { get; init; }
            public List<Guid> RolIds { get; init; } = new();
        }
    }
}

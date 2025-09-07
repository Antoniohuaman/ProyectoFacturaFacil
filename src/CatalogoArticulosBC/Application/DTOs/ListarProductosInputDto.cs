using System;

namespace CatalogoArticulosBC.Application.UseCases.ListarProductos
{
    /// <summary>
    /// Criterios de búsqueda, ordenamiento y paginación para listar productos.
    /// </summary>
    public sealed class ListarProductosInputDto
    {
        // Filtros
        public string? Nombre { get; init; }
        /// <summary>Nombre exacto de la categoría (debe mapear a VO Categoria).</summary>
        public string? Categoria { get; init; }
        public bool? Habilitado { get; init; }
        public decimal? PrecioMin { get; init; }
        public decimal? PrecioMax { get; init; }

        // Ordenamiento
        /// <summary>Campo de orden: nombre | sku | categoria | habilitado. Default: nombre</summary>
        public string? OrdenarPor { get; init; }
        /// <summary>asc | desc. Default: asc</summary>
        public string? Direccion { get; init; }

        // Paginación
        /// <summary>Página 1-based. Si es &lt; 1 se normaliza a 1.</summary>
        public int Page { get; init; } = 1;
        /// <summary>Tamaño de página. Rango recomendado 1..200. Default: 20.</summary>
        public int PageSize { get; init; } = 20;
    }
}

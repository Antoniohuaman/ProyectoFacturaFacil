using System;

namespace CatalogoArticulosBC.Application.UseCases.EliminarProductosPorCriterio
{
    /// <summary>
    /// Criterios para eliminar productos por filtro/búsqueda (no por página).
    /// Requiere confirmación explícita y al menos un criterio.
    /// </summary>
    public sealed class EliminarProductosPorCriterioInputDto
    {
        /// <summary>Debe ser true para proceder.</summary>
        public bool Confirmar { get; init; }

        /// <summary>Nombre que contenga (match contains, sensible a tu repo).</summary>
        public string? NombreContiene { get; init; }

        /// <summary>Nombre de la categoría (se mapeará a VO Categoria si se indica).</summary>
        public string? CategoriaNombre { get; init; }

        /// <summary>Filtra por habilitado/deshabilitado si se indica.</summary>
        public bool? Habilitado { get; init; }

        /// <summary>Precio mínimo (si se indica).</summary>
        public decimal? PrecioMin { get; init; }

        /// <summary>Precio máximo (si se indica).</summary>
        public decimal? PrecioMax { get; init; }

        /// <summary>
        /// Valida que haya al menos un criterio.
        /// </summary>
        public bool TieneAlMenosUnCriterio() =>
            !string.IsNullOrWhiteSpace(NombreContiene)
            || !string.IsNullOrWhiteSpace(CategoriaNombre)
            || Habilitado.HasValue
            || PrecioMin.HasValue
            || PrecioMax.HasValue;
    }
}

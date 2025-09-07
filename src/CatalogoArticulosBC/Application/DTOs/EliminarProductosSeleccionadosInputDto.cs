using System;
using System.Collections.Generic;

namespace CatalogoArticulosBC.Application.UseCases.EliminarProductosSeleccionados
{
    /// <summary>
    /// Petición para eliminar productos seleccionados de la empresa actual.
    /// Puedes pasar IDs y/o SKUs. Debe venir al menos uno.
    /// </summary>
    public sealed class EliminarProductosSeleccionadosInputDto
    {
        /// <summary>Confirmación explícita del usuario para proceder.</summary>
        public bool Confirmar { get; init; }

        /// <summary>Ids (ProductoId) seleccionados para eliminar.</summary>
        public IReadOnlyCollection<Guid>? ProductoIds { get; init; }

        /// <summary>SKUs seleccionados para eliminar (an..30, normalizados por el VO).</summary>
        public IReadOnlyCollection<string>? Skus { get; init; }
    }
}

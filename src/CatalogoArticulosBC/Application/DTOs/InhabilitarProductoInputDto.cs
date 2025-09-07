using System;

namespace CatalogoArticulosBC.Application.UseCases.InhabilitarProducto
{
    /// <summary>
    /// Entrada para inhabilitar un producto específico.
    /// </summary>
    public sealed class InhabilitarProductoInputDto
    {
        /// <summary>
        /// Identificador del producto a inhabilitar.
        /// </summary>
        public Guid ProductoId { get; init; }

        /// <summary>
        /// Motivo de inhabilitación (obligatorio).
        /// </summary>
        public string Motivo { get; init; } = string.Empty;
    }
}

using System;

namespace CatalogoArticulosBC.Application.UseCases.HabilitarProducto
{
    /// <summary>
    /// Entrada para habilitar un producto previamente inhabilitado.
    /// </summary>
    public sealed class HabilitarProductoInputDto
    {
        /// <summary>Identificador del producto a habilitar.</summary>
        public Guid ProductoId { get; init; }

        /// <summary>Usuario que realiza la habilitación (obligatorio).</summary>
        public string Usuario { get; init; } = string.Empty;

        /// <summary>Motivo opcional de la habilitación (auditoría).</summary>
        public string? Motivo { get; init; }
    }
}

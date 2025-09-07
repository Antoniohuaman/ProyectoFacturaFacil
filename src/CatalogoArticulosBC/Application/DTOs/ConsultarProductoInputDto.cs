using System;

namespace CatalogoArticulosBC.Application.UseCases.ConsultarProducto
{
    /// <summary>
    /// Parámetros para consultar el detalle de un producto.
    /// Puedes enviar ProductoId, o Sku, o Nombre (en ese orden de prioridad).
    /// </summary>
    public sealed class ConsultarProductoInputDto
    {
        /// <summary>Identificador del producto (prioridad 1).</summary>
        public Guid? ProductoId { get; init; }

        /// <summary>SKU del producto (prioridad 2).</summary>
        public string? Sku { get; init; }

        /// <summary>Nombre exacto del producto (prioridad 3).</summary>
        public string? Nombre { get; init; }

        /// <summary>Incluir archivos multimedia en la respuesta (por defecto true).</summary>
        public bool IncluirMultimedia { get; init; } = true;
    }
}

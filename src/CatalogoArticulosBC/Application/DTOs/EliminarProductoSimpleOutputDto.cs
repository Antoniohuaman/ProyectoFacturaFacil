using System;

namespace CatalogoArticulosBC.Application.UseCases.EliminarProductoSimple
{
    /// <summary>
    /// Resultado de la eliminación del producto.
    /// </summary>
    public sealed class EliminarProductoSimpleOutputDto
    {
        public Guid ProductoId { get; init; }
        public string Sku { get; init; } = default!;
        public string Nombre { get; init; } = default!;
        public bool Eliminado { get; init; }
    }
}

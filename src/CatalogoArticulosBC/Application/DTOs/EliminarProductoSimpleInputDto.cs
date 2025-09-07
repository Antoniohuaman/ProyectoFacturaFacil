using System;

namespace CatalogoArticulosBC.Application.UseCases.EliminarProductoSimple
{
    /// <summary>
    /// Datos mínimos para eliminar un producto simple.
    /// </summary>
    public sealed class EliminarProductoSimpleInputDto
    {
        public Guid ProductoId { get; init; }
    }
}

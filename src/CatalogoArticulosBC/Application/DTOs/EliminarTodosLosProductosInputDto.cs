using System;

namespace CatalogoArticulosBC.Application.UseCases.EliminarTodosLosProductos
{
    /// <summary>
    /// Entrada para eliminación masiva de productos de la empresa actual.
    /// Se exige confirmación explícita para prevenir borrados accidentales.
    /// </summary>
    public sealed class EliminarTodosLosProductosInputDto
    {
        /// <summary>
        /// Debe ser true para proceder con el vaciado total (hard delete).
        /// </summary>
        public bool Confirmar { get; init; }
    }
}

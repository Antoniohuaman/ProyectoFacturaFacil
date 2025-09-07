using System;

namespace CatalogoArticulosBC.Application.UseCases.EliminarTodosLosProductos
{
    /// <summary>
    /// Resultado de la eliminación masiva de productos.
    /// </summary>
    public sealed class EliminarTodosLosProductosOutputDto
    {
        /// <summary>Empresa/tenant afectado (string interno del VO EmpresaId).</summary>
        public string EmpresaId { get; init; } = default!;

        /// <summary>Cantidad total de productos eliminados.</summary>
        public int CantidadEliminada { get; init; }

        /// <summary>Marca de tiempo (UTC) de la ejecución.</summary>
        public DateTimeOffset EjecutadoEnUtc { get; init; }

        /// <summary>Indicador de ejecución exitosa.</summary>
        public bool Exitoso { get; init; }
    }
}

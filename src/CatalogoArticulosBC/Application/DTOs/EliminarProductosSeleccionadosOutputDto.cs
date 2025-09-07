using System;
using System.Collections.Generic;

namespace CatalogoArticulosBC.Application.UseCases.EliminarProductosSeleccionados
{
    /// <summary>
    /// Resultado de la eliminación de una selección de productos.
    /// </summary>
    public sealed class EliminarProductosSeleccionadosOutputDto
    {
        public string EmpresaId { get; init; } = default!;
        public int CantidadSolicitada { get; init; }
        public int CantidadEliminada { get; init; }
        public int CantidadNoEncontrada { get; init; }
        public IReadOnlyCollection<Guid> IdsEliminados { get; init; } = Array.Empty<Guid>();
        public IReadOnlyCollection<Guid> IdsNoEncontrados { get; init; } = Array.Empty<Guid>();
        public IReadOnlyCollection<string> SkusNoEncontrados { get; init; } = Array.Empty<string>();
        public DateTimeOffset EjecutadoEnUtc { get; init; }
        public bool Exitoso { get; init; }
    }
}

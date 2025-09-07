using System;
using System.Collections.Generic;

namespace CatalogoArticulosBC.Application.UseCases.EliminarProductosPorCriterio
{
    public sealed class EliminarProductosPorCriterioOutputDto
    {
        public string EmpresaId { get; init; } = default!;

        public sealed class CriterioEcho
        {
            public string? NombreContiene { get; init; }
            public string? CategoriaNombre { get; init; }
            public bool? Habilitado { get; init; }
            public decimal? PrecioMin { get; init; }
            public decimal? PrecioMax { get; init; }
        }

        public CriterioEcho Criterio { get; init; } = new();
        public int CantidadCoincidente { get; init; }
        public int CantidadEliminada { get; init; }
        public IReadOnlyCollection<Guid> IdsEliminados { get; init; } = Array.Empty<Guid>();
        public DateTimeOffset EjecutadoEnUtc { get; init; }
        public bool Exitoso { get; init; }
    }
}

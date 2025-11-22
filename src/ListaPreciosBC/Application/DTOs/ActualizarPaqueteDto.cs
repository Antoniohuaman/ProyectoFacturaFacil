using System;
using System.Collections.Generic;

namespace ListaPreciosBC.Application.DTOs
{
    public sealed class ActualizarPaqueteDto
    {
        public Guid PaqueteId { get; init; }
        public string Nombre { get; init; } = string.Empty;
        public string? Descripcion { get; init; }
        public decimal DescuentoPorcentaje { get; init; }

        public IReadOnlyCollection<PaqueteProductoLineaDto> Productos { get; init; } =
            new List<PaqueteProductoLineaDto>();
    }
}

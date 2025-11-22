using System;
using System.Collections.Generic;

namespace ListaPreciosBC.Application.DTOs
{
    /// <summary>
    /// DTO para ver/editar el detalle de un paquete.
    /// </summary>
    public sealed class PaqueteDetalleDto
    {
        public Guid PaqueteId { get; init; }
        public string Nombre { get; init; } = string.Empty;
        public string? Descripcion { get; init; }
        public decimal DescuentoPorcentaje { get; init; }
        public DateTime FechaCreacionUtc { get; init; }

        public IReadOnlyCollection<PaqueteProductoLineaDto> Productos { get; init; } =
            new List<PaqueteProductoLineaDto>();
    }
}

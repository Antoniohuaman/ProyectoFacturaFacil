using System.Collections.Generic;
using ListaPreciosBC.Domain.Aggregates;

namespace ListaPreciosBC.Application.DTOs
{
    public sealed class CrearPaqueteDto
    {
        public string Nombre { get; init; } = string.Empty;
        public string? Descripcion { get; init; }
        public decimal DescuentoPorcentaje { get; init; }

        /// <summary>
        /// Productos incluidos en el paquete.
        /// </summary>
        public IReadOnlyCollection<ProductoPaquete.LineaProductoPaquete> Productos { get; init; } =
            new List<ProductoPaquete.LineaProductoPaquete>();
    }
}

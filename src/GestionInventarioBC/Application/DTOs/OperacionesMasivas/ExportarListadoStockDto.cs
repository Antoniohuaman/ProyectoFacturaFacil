using System;
using System.Collections.Generic;

namespace GestionInventarioBC.Application.DTOs.OperacionesMasivas
{
    public class ExportarListadoStockDto
    {
        public Guid EstablecimientoId { get; set; }
        public Guid AlmacenId { get; set; }
    }

    public class ExportarListadoStockItemDto
    {
        public Guid ProductoId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public decimal Real { get; set; }
        public decimal Reservado { get; set; }
        public decimal Disponible { get; set; }
    }

    public class ExportarListadoStockResultDto
    {
        public List<ExportarListadoStockItemDto> Items { get; set; } = new();
    }
}

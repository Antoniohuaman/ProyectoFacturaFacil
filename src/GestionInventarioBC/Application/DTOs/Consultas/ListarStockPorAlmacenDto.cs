using System;
using System.Collections.Generic;

namespace GestionInventarioBC.Application.DTOs.Consultas
{
    public class ListarStockPorAlmacenDto
    {
        public Guid EstablecimientoId { get; set; }
        public Guid AlmacenId { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }

    public class ListarStockPorAlmacenItemDto
    {
        public Guid ProductoId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public decimal Real { get; set; }
        public decimal Reservado { get; set; }
        public decimal Disponible { get; set; }
    }

    public class ListarStockPorAlmacenResultDto
    {
        public int Total { get; set; }
        public List<ListarStockPorAlmacenItemDto> Items { get; set; } = new();
    }
}

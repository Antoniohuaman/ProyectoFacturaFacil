using System;
using System.Collections.Generic;

namespace GestionInventarioBC.Application.DTOs.Consultas
{
    public class GenerarKardexPorProductoDto
    {
        public Guid EstablecimientoId { get; set; }
        public Guid AlmacenId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public DateTimeOffset? Desde { get; set; }
        public DateTimeOffset? Hasta { get; set; }
    }

    public class GenerarKardexPorProductoItemDto
    {
        public DateTimeOffset Fecha { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public decimal Entrada { get; set; }
        public decimal Salida { get; set; }
        public decimal SaldoAcumulado { get; set; }
    }

    public class GenerarKardexPorProductoResultDto
    {
        public List<GenerarKardexPorProductoItemDto> Movimientos { get; set; } = new();
    }
}

using System;
using System.Collections.Generic;

namespace GestionInventarioBC.Application.DTOs.OperacionesMasivas
{
    public class ActualizarStockMasivoDto
    {
        public Guid EstablecimientoId { get; set; }
        public Guid AlmacenId { get; set; }
        public List<ActualizarStockMasivoLineaDto> Lineas { get; set; } = new();
    }

    public class ActualizarStockMasivoLineaDto
    {
        public string? Sku { get; set; }
        public Guid? ProductoId { get; set; }
        public decimal Cantidad { get; set; }
    }

    public class ActualizarStockMasivoResultDto
    {
        public int Procesados { get; set; }
    }
}

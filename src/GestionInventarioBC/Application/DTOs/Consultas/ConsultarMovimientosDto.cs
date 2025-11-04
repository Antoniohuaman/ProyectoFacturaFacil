using System;
using System.Collections.Generic;

namespace GestionInventarioBC.Application.DTOs.Consultas
{
    public class ConsultarMovimientosDto
    {
        public Guid EstablecimientoId { get; set; }
        public Guid AlmacenId { get; set; }
        public DateTimeOffset? Desde { get; set; }
        public DateTimeOffset? Hasta { get; set; }
        public string? Sku { get; set; }
        public string? Tipo { get; set; }
        public string? Motivo { get; set; }
    }

    public class ConsultarMovimientosLineaDto
    {
        public string Sku { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
    }

    public class ConsultarMovimientosItemDto
    {
        public Guid MovimientoId { get; set; }
        public DateTimeOffset Fecha { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
        public List<ConsultarMovimientosLineaDto> Lineas { get; set; } = new();
    }

    public class ConsultarMovimientosResultDto
    {
        public List<ConsultarMovimientosItemDto> Movimientos { get; set; } = new();
    }
}

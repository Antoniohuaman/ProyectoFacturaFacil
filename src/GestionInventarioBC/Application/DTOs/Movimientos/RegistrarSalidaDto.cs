using System;
using System.Collections.Generic;

namespace GestionInventarioBC.Application.DTOs.Movimientos
{
    public class RegistrarSalidaDto
    {
        public Guid EstablecimientoId { get; set; }
        public Guid AlmacenId { get; set; }
        public DateTimeOffset Fecha { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public List<RegistrarSalidaLineaDto> Lineas { get; set; } = new();
    }

    public class RegistrarSalidaLineaDto
    {
        public string? Sku { get; set; }
        public Guid? ProductoId { get; set; }
        public decimal Cantidad { get; set; }
    }

    public class RegistrarSalidaResultDto
    {
        public Guid MovimientoId { get; set; }
        public int LineasAfectadas { get; set; }
    }
}

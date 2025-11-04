using System;
using System.Collections.Generic;

namespace GestionInventarioBC.Application.DTOs.Movimientos
{
    public class RegistrarIngresoDto
    {
        public Guid EstablecimientoId { get; set; }
        public Guid AlmacenId { get; set; }
        public DateTimeOffset Fecha { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public List<RegistrarIngresoLineaDto> Lineas { get; set; } = new();
    }

    public class RegistrarIngresoLineaDto
    {
        public string Sku { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
    }

    public class RegistrarIngresoResultDto
    {
        public Guid MovimientoId { get; set; }
        public int LineasAfectadas { get; set; }
    }
}

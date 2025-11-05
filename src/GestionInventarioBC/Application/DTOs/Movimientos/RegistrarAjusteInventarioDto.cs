using System;
using System.Collections.Generic;

namespace GestionInventarioBC.Application.DTOs.Movimientos
{
    public class RegistrarAjusteInventarioDto
    {
        public Guid EstablecimientoId { get; set; }
        public Guid AlmacenId { get; set; }
        public DateTimeOffset Fecha { get; set; }
        public List<RegistrarAjusteInventarioItemDto> Items { get; set; } = new();
    }

    public class RegistrarAjusteInventarioItemDto
    {
        public string? Sku { get; set; }
        public Guid? ProductoId { get; set; }
        public decimal Delta { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }

    public class RegistrarAjusteInventarioResultDto
    {
        public Guid MovimientoId { get; set; }
        public int LineasAfectadas { get; set; }
    }
}

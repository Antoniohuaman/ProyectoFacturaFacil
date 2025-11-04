using System;

namespace GestionInventarioBC.Application.DTOs.Consultas
{
    public class ConsultarDisponibilidadProductoDto
    {
        public Guid EstablecimientoId { get; set; }
        public Guid AlmacenId { get; set; }
        public string Sku { get; set; } = string.Empty;
    }

    public class ConsultarDisponibilidadProductoResultDto
    {
        public string Sku { get; set; } = string.Empty;
        public decimal Real { get; set; }
        public decimal Reservado { get; set; }
        public decimal Disponible { get; set; }
    }
}

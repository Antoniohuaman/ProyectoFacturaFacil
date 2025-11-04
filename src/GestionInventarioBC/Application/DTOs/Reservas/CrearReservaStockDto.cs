using System;

namespace GestionInventarioBC.Application.DTOs.Reservas
{
    public class CrearReservaStockDto
    {
        public Guid EstablecimientoId { get; set; }
        public Guid AlmacenId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public DateTimeOffset? VenceEn { get; set; }
    }

    public class CrearReservaStockResultDto
    {
        public Guid ReservaId { get; set; }
    }
}

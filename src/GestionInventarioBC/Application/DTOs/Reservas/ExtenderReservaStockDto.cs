using System;

namespace GestionInventarioBC.Application.DTOs.Reservas
{
    public class ExtenderReservaStockDto
    {
        public Guid EstablecimientoId { get; set; }
        public Guid AlmacenId { get; set; }
        public Guid ReservaId { get; set; }
        public DateTimeOffset NuevaFechaVencimiento { get; set; }
    }

    public class ExtenderReservaStockResultDto
    {
        public bool Ok { get; set; }
    }
}

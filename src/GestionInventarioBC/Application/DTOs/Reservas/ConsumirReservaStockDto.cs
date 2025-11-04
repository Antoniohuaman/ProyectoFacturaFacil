using System;

namespace GestionInventarioBC.Application.DTOs.Reservas
{
    public class ConsumirReservaStockDto
    {
        public Guid EstablecimientoId { get; set; }
        public Guid AlmacenId { get; set; }
        public Guid ReservaId { get; set; }
    }

    public class ConsumirReservaStockResultDto
    {
        public bool Ok { get; set; }
    }
}

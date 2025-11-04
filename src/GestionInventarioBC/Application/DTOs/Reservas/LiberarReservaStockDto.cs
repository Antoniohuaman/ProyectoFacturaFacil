using System;

namespace GestionInventarioBC.Application.DTOs.Reservas
{
    public class LiberarReservaStockDto
    {
        public Guid EstablecimientoId { get; set; }
        public Guid AlmacenId { get; set; }
        public Guid ReservaId { get; set; }
    }

    public class LiberarReservaStockResultDto
    {
        public bool Ok { get; set; }
    }
}

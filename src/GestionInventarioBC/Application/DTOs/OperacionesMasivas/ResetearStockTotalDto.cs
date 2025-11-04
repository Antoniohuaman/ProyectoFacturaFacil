using System;

namespace GestionInventarioBC.Application.DTOs.OperacionesMasivas
{
    public class ResetearStockTotalDto
    {
        public Guid EstablecimientoId { get; set; }
        public Guid AlmacenId { get; set; }
    }

    public class ResetearStockTotalResultDto
    {
        public int Afectados { get; set; }
    }
}

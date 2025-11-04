using System;

namespace GestionInventarioBC.Application.DTOs.Movimientos
{
    public class AnularMovimientoInventarioDto
    {
        public Guid EstablecimientoId { get; set; }
        public Guid AlmacenId { get; set; }
        public Guid MovimientoId { get; set; }
        public DateTimeOffset Fecha { get; set; }
    }

    public class AnularMovimientoInventarioResultDto
    {
        public Guid MovimientoCompensatorioId { get; set; }
    }
}

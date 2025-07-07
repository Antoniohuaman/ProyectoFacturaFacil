using System;
using ControlCajaBC.Domain.ValueObjects;

namespace ControlCajaBC.Application.UseCases
{
    /// <summary>
    /// Resultado de la consulta de historial: un movimiento con sus datos.
    /// </summary>
    public sealed class MovimientoDto
    {
        public MovimientoDto(
            Guid   id,
            DateTime fechaHora,
            decimal monto,
            TipoMovimiento tipo)
        {
            Id         = id;
            FechaHora  = fechaHora;
            Monto      = monto;
            Tipo       = tipo;
        }

        public Guid            Id        { get; }
        public DateTime        FechaHora { get; }
        public decimal         Monto     { get; }
        public TipoMovimiento  Tipo      { get; }
    }
}

using System;

using ControlCajaBC.Domain.ValueObjects;

namespace ControlCajaBC.Domain.Entities
{
    /// <summary>
    /// Representa un movimiento de caja (ingreso o egreso).
    /// </summary>
    public class MovimientoCaja
    {
        public Guid Id { get; }
        public CodigoCaja CodigoCaja { get; }
        public FechaHora FechaHora { get; }
        public Monto Monto { get; }
        public TipoMovimiento Tipo { get; }

        public MovimientoCaja(
            Guid id,
            CodigoCaja codigoCaja,
            FechaHora fechaHora,
            Monto monto,
            TipoMovimiento tipo)
        {
            Id = id != Guid.Empty ? id : throw new ArgumentException("El Id no puede ser vacío.", nameof(id));
            CodigoCaja = codigoCaja ?? throw new ArgumentNullException(nameof(codigoCaja));
            FechaHora = fechaHora ?? throw new ArgumentNullException(nameof(fechaHora));
            Monto = monto ?? throw new ArgumentNullException(nameof(monto));
            Tipo = tipo;
        }
    }
}

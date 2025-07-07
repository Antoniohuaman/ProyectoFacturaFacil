// src/ControlCajaBC/Domain/Events/MovimientoRegistrado.cs

using System;
using ControlCajaBC.Domain.Entities;

namespace ControlCajaBC.Domain.Events
{
    /// <summary>
    /// Evento que se dispara cuando se registra un movimiento en el turno.
    /// </summary>
    public sealed class MovimientoRegistrado : IDomainEvent
    {
        public MovimientoCaja Movimiento { get; }
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public MovimientoRegistrado(MovimientoCaja movimiento)
        {
            Movimiento = movimiento 
                ?? throw new ArgumentNullException(nameof(movimiento));
        }
    }
}

// src/ControlCajaBC/Domain/Events/MovimientoRegistrado.cs

using System;
using ControlCajaBC.Domain.Entities;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace ControlCajaBC.Domain.Events
{
    /// <summary>
    /// Evento que se dispara cuando se registra un movimiento en el turno.
    /// </summary>
    public sealed class MovimientoRegistrado : IDomainEvent
    {
        public EmpresaId EmpresaId { get; }
        public EstablecimientoId? EstablecimientoId { get; }
        public MovimientoCaja Movimiento { get; }
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public MovimientoRegistrado(EmpresaId empresaId, EstablecimientoId? establecimientoId, MovimientoCaja movimiento)
        {
            EmpresaId = empresaId ?? throw new ArgumentNullException(nameof(empresaId));
            EstablecimientoId = establecimientoId;
            Movimiento = movimiento 
                ?? throw new ArgumentNullException(nameof(movimiento));
        }
    }
}

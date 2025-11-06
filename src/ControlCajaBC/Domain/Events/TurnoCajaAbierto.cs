using System;
using ControlCajaBC.Domain.ValueObjects;
using ControlCajaBC.Domain.Entities;
using SharedKernel.ValueObjects;
using SharedKernel.Events;


namespace ControlCajaBC.Domain.Events
{
    /// <summary>
    /// Evento que se dispara cuando se abre un turno de caja.
    /// </summary>
    public sealed class TurnoCajaAbierto : IDomainEvent
    {
        public CodigoCaja CodigoCaja { get; }
        public EmpresaId EmpresaId { get; }
        public EstablecimientoId? EstablecimientoId { get; }
        public FechaHora FechaApertura { get; }
        public ResponsableCaja Responsable { get; }
        public Monto SaldoInicial { get; }

        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public TurnoCajaAbierto(
            CodigoCaja codigoCaja,
            EmpresaId empresaId,
            EstablecimientoId? establecimientoId,
            FechaHora fechaApertura,
            ResponsableCaja responsable,
            Monto saldoInicial)
        {
            CodigoCaja = codigoCaja;
            EmpresaId = empresaId;
            EstablecimientoId = establecimientoId;
            FechaApertura = fechaApertura;
            Responsable = responsable;
            SaldoInicial = saldoInicial;
        }
    }
}

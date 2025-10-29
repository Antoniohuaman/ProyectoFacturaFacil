// src/ControlCajaBC/Domain/Events/TurnoCajaCerrado.cs

using System;
using ControlCajaBC.Domain.Entities;
using ControlCajaBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;
namespace ControlCajaBC.Domain.Events
{
    /// <summary>
    /// Evento que se dispara cuando se cierra un turno de caja.
    /// </summary>
    public sealed class TurnoCajaCerrado : IDomainEvent
    {
        public CodigoCaja CodigoCaja { get; }
        public EmpresaId EmpresaId { get; }
        public EstablecimientoId? EstablecimientoId { get; }
        public FechaHora FechaCierre { get; }
        public ResponsableCaja ResponsableCierre { get; }
        public Monto SaldoFinal { get; }
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public TurnoCajaCerrado(
            CodigoCaja codigoCaja,
            EmpresaId empresaId,
            EstablecimientoId? establecimientoId,
            FechaHora fechaCierre,
            ResponsableCaja responsableCierre,
            Monto saldoFinal)
        {
            CodigoCaja = codigoCaja 
                ?? throw new ArgumentNullException(nameof(codigoCaja));
            EmpresaId = empresaId ?? throw new ArgumentNullException(nameof(empresaId));
            EstablecimientoId = establecimientoId;
            FechaCierre = fechaCierre 
                ?? throw new ArgumentNullException(nameof(fechaCierre));
            ResponsableCierre = responsableCierre 
                ?? throw new ArgumentNullException(nameof(responsableCierre));
            SaldoFinal = saldoFinal 
                ?? throw new ArgumentNullException(nameof(saldoFinal));
        }
    }
}

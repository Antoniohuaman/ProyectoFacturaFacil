// src/ControlCajaBC/Domain/Aggregates/TurnoCaja.cs

using System;
using System.Collections.Generic;
using System.Linq;
using ControlCajaBC.Domain.Entities;
using ControlCajaBC.Domain.Events;
using ControlCajaBC.Domain.ValueObjects;

namespace ControlCajaBC.Domain.Aggregates
{
    /// <summary>
    /// Agregado raíz que representa un turno en la caja.
    /// </summary>
    public class TurnoCaja
    {
        private readonly List<MovimientoCaja> _movimientos = new();

        public CodigoCaja CodigoCaja { get; }
        public FechaHora FechaApertura { get; }
        public ResponsableCaja ResponsableApertura { get; }
        public Monto SaldoInicial { get; private set; }

        /// <summary>
        /// Responsable que finalmente cierra el turno (si aplica).
        /// </summary>
        public ResponsableCaja? ResponsableCierre { get; private set; }

        /// <summary>
        /// Fecha y hora de cierre del turno (si aplica).
        /// </summary>
        public FechaHora? FechaCierre { get; private set; }

        public IReadOnlyCollection<MovimientoCaja> Movimientos => _movimientos.AsReadOnly();

        /// <summary>
        /// Calcula el saldo actual sumando ingresos y restando egresos.
        /// </summary>
        public Monto SaldoActual
        {
            get
            {
                var total = SaldoInicial;
                foreach (var m in _movimientos)
                {
                    total = m.Tipo == TipoMovimiento.Ingreso
                        ? total.Add(m.Monto)
                        : total.Subtract(m.Monto);
                }
                return total;
            }
        }

        public bool EstaAbierto { get; private set; } = true;

        public bool EstaCerrado => !EstaAbierto;

        public TurnoCaja(
            CodigoCaja codigoCaja,
            FechaHora fechaApertura,
            ResponsableCaja responsable,
            Monto saldoInicial)
        {
            CodigoCaja = codigoCaja  ?? throw new ArgumentNullException(nameof(codigoCaja));
            FechaApertura = fechaApertura  ?? throw new ArgumentNullException(nameof(fechaApertura));
            ResponsableApertura = responsable  ?? throw new ArgumentNullException(nameof(responsable));
            SaldoInicial = saldoInicial  ?? throw new ArgumentNullException(nameof(saldoInicial));

            var abierto = new TurnoCajaAbierto(codigoCaja, fechaApertura, responsable, saldoInicial);
            // aquí publicas 'abierto' (IDomainEventDispatcher.Dispatch(abierto))
        }

        /// <summary>
        /// Registra un movimiento en el turno (ingreso o egreso).
        /// </summary>
        public void RegistrarMovimiento(MovimientoCaja movimiento)
        {
            if (!EstaAbierto)
                throw new InvalidOperationException("No se puede registrar movimientos en un turno cerrado.");

            _movimientos.Add(movimiento);

            var registrado = new MovimientoRegistrado(movimiento);
            // Dispatch(registrado)
        }

        /// <summary>
        /// Anula un movimiento previamente registrado (por su Id).
        /// </summary>
        public void AnularMovimiento(Guid movimientoId)
        {
            if (!EstaAbierto)
                throw new InvalidOperationException("No se puede anular movimientos en un turno cerrado.");

            var mov = _movimientos.SingleOrDefault(m => m.Id == movimientoId)
                      ?? throw new InvalidOperationException("Movimiento no encontrado.");

            _movimientos.Remove(mov);

            // Puedes emitir aquí un evento MovimientoAnulado si lo defines en tu doc.
        }

        /// <summary>
        /// Ajusta el saldo inicial del turno a un nuevo valor.
        /// </summary>
        public void AjustarSaldo(Monto nuevoSaldo)
        {
            SaldoInicial = nuevoSaldo ?? throw new ArgumentNullException(nameof(nuevoSaldo));

            // Puedes emitir aquí un evento SaldoAjustado si lo defines en tu doc.
        }

        /// <summary>
        /// Cierra el turno de caja, dejando de aceptar movimientos.
        /// </summary>
        public void CerrarTurno(FechaHora fechaCierre, ResponsableCaja responsableCierre)
        {
            if (!EstaAbierto)
                throw new InvalidOperationException("El turno ya está cerrado.");

            FechaCierre = fechaCierre  ?? throw new ArgumentNullException(nameof(fechaCierre));
            ResponsableCierre = responsableCierre  ?? throw new ArgumentNullException(nameof(responsableCierre));
            EstaAbierto = false;

            var cerrado = new TurnoCajaCerrado(CodigoCaja, fechaCierre, responsableCierre, SaldoActual);
            // Dispatch(cerrado)
        }

        /// <summary>
        /// Permite delegar el cierre del turno a otra persona.
        /// </summary>
        public void DelegarCierre(ResponsableCaja nuevoResponsable)
        {
            if (!EstaAbierto)
                throw new InvalidOperationException("El turno ya está cerrado.");

            ResponsableCierre = nuevoResponsable ?? throw new ArgumentNullException(nameof(nuevoResponsable));
            // Puedes emitir aquí un evento CierreDelegado si lo defines.
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ControlCajaBC.Domain.Aggregates;
using ControlCajaBC.Domain.Entities;
using ControlCajaBC.Domain.ValueObjects;

namespace ControlCajaBC.Application.Interfaces
{
    /// <summary>
    /// Puerto de salida para persistir y recuperar Turnos de Caja.
    /// </summary>
    public interface IControlCajaRepository
    {
        /// <summary>
        /// Obtiene el turno de caja abierto para la caja indicada, o null si no hay ninguno.
        /// </summary>
        Task<TurnoCaja?> GetTurnoAbiertoAsync(CodigoCaja codigoCaja);

        /// <summary>
        /// Agrega un nuevo Turno de Caja.
        /// </summary>
        Task AddTurnoCajaAsync(TurnoCaja turno);

        /// <summary>
        /// Actualiza el Turno de Caja (por ejemplo, para añadir movimientos o marcar cierre).
        /// </summary>
        Task UpdateTurnoCajaAsync(TurnoCaja turno);

        /// <summary>
        /// Obtiene todos los movimientos (ingresos/egresos) de un turno.
        /// </summary>
        Task<IReadOnlyCollection<MovimientoCaja>> GetMovimientosAsync(CodigoCaja codigoCaja);
        /// <summary>
        /// Obtiene el turno de caja cerrado para la caja indicada, o null si no hay ninguno.
        /// </summary>
        Task<TurnoCaja?> GetTurnoCerradoAsync(CodigoCaja codigoCaja);

    }
}

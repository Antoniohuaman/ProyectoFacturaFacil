using System;
using System.Threading.Tasks;
using ControlCajaBC.Application.Interfaces;
using ControlCajaBC.Domain.Aggregates;
using ControlCajaBC.Domain.ValueObjects;

namespace ControlCajaBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso: anular un movimiento de un turno abierto.
    /// </summary>
    public class AnularMovimientoUseCase
    {
        private readonly IControlCajaRepository _repo;
        private readonly IUnitOfWork _uow;

        public AnularMovimientoUseCase(
            IControlCajaRepository repo,
            IUnitOfWork uow)
        {
            _repo = repo;
            _uow  = uow;
        }

        /// <summary>
        /// Ejecuta la anulación de un movimiento identificado por su Id.
        /// </summary>
        public async Task HandleAsync(
            CodigoCaja codigoCaja,
            Guid       movimientoId)
        {
            // 1. Obtener turno abierto
            var turno = await _repo.GetTurnoAbiertoAsync(codigoCaja)
                        ?? throw new InvalidOperationException(
                               $"No existe un turno abierto para la caja {codigoCaja.Value}.");

            // 2. Anular
            turno.AnularMovimiento(movimientoId);

            // 3. Persistir y confirmar
            await _repo.UpdateTurnoCajaAsync(turno);
            await _uow.CommitAsync();
        }
    }
}

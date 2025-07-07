using System;
using System.Threading.Tasks;
using ControlCajaBC.Application.Interfaces;
using ControlCajaBC.Domain.Aggregates;
using ControlCajaBC.Domain.ValueObjects;

namespace ControlCajaBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso: ajustar el saldo inicial de un turno abierto.
    /// </summary>
    public class AjustarSaldoUseCase
    {
        private readonly IControlCajaRepository _repo;
        private readonly IUnitOfWork _uow;

        public AjustarSaldoUseCase(IControlCajaRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow  = uow;
        }

        /// <summary>
        /// Ejecuta el ajuste de saldo.
        /// </summary>
        public async Task HandleAsync(CodigoCaja codigoCaja, Monto nuevoSaldo)
        {
            // 1. Obtener turno abierto o error
            var turno = await _repo.GetTurnoAbiertoAsync(codigoCaja)
                        ?? throw new InvalidOperationException(
                               $"No existe un turno abierto para la caja {codigoCaja.Value}.");

            // 2. Ajustar saldo
            turno.AjustarSaldo(nuevoSaldo);

            // 3. Persistir y confirmar
            await _repo.UpdateTurnoCajaAsync(turno);
            await _uow.CommitAsync();
        }
    }
}

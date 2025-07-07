using System;
using System.Threading.Tasks;
using ControlCajaBC.Application.Interfaces;
using ControlCajaBC.Domain.Aggregates;
using ControlCajaBC.Domain.Entities;
using ControlCajaBC.Domain.ValueObjects;

namespace ControlCajaBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso: registrar un ingreso o egreso en un turno abierto.
    /// </summary>
    public class RegistrarMovimientoUseCase
    {
        private readonly IControlCajaRepository _repo;
        private readonly IUnitOfWork            _uow;

        public RegistrarMovimientoUseCase(IControlCajaRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow  = uow;
        }

        /// <summary>
        /// Ejecuta el registro de un movimiento en caja.
        /// </summary>
        /// <returns>El Id del movimiento recién creado.</returns>
        public async Task<Guid> HandleAsync(
            CodigoCaja     codigoCaja,
            FechaHora      fechaMovimiento,
            Monto          monto,
            TipoMovimiento tipo)
        {
            // 1) Recuperar turno abierto
            var turno = await _repo.GetTurnoAbiertoAsync(codigoCaja)
                        ?? throw new InvalidOperationException(
                               $"No existe un turno abierto para la caja {codigoCaja.Value}.");

            // 2) Crear y registrar movimiento
            var movimientoId = Guid.NewGuid();
            var movimiento = new MovimientoCaja(
                movimientoId,
                codigoCaja,
                fechaMovimiento,
                monto,
                tipo);

            turno.RegistrarMovimiento(movimiento);

            // 3) Persistir el agregado modificado
            await _repo.UpdateTurnoCajaAsync(turno);
            await _uow.CommitAsync();

            return movimientoId;
        }
    }
}

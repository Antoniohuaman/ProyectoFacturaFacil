using System;
using System.Threading.Tasks;
using ControlCajaBC.Application.Interfaces;
using ControlCajaBC.Domain.Aggregates;
using ControlCajaBC.Domain.Entities; 
using ControlCajaBC.Domain.ValueObjects;

namespace ControlCajaBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso: cerrar el turno de caja.
    /// </summary>
    public class CerrarTurnoUseCase
    {
        private readonly IControlCajaRepository _repo;
        private readonly IUnitOfWork _uow;

        public CerrarTurnoUseCase(
            IControlCajaRepository repo,
            IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        /// <summary>
        /// Ejecuta el cierre de turno.
        /// </summary>
        /// <param name="codigoCaja">Código de la caja.</param>
        /// <param name="fechaCierre">Fecha y hora UTC de cierre.</param>
        /// <param name="responsableCierre">Usuario que cierra el turno.</param>
        public async Task HandleAsync(
            CodigoCaja codigoCaja,
            FechaHora fechaCierre,
            ResponsableCaja responsableCierre)
        {
            // 1. Obtener turno abierto
            var turno = await _repo.GetTurnoAbiertoAsync(codigoCaja)
                         ?? throw new InvalidOperationException(
                                $"No existe un turno abierto para la caja {codigoCaja.Value}.");

            // 2. Cerrar turno
            turno.CerrarTurno(fechaCierre, responsableCierre);

            // 3. Persistir cambios
            await _repo.UpdateTurnoCajaAsync(turno);
            await _uow.CommitAsync();
        }
    }
}

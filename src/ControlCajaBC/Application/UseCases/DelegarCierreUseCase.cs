using System;
using System.Threading.Tasks;
using ControlCajaBC.Application.Interfaces;
using ControlCajaBC.Domain.ValueObjects;
using ControlCajaBC.Domain.Entities;

namespace ControlCajaBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso: delegar el cierre del turno a otro responsable.
    /// </summary>
    public class DelegarCierreUseCase
    {
        private readonly IControlCajaRepository _repo;
        private readonly IUnitOfWork _uow;

        public DelegarCierreUseCase(IControlCajaRepository repo, IUnitOfWork uow)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow  = uow  ?? throw new ArgumentNullException(nameof(uow));
        }

        /// <summary>
        /// Ejecuta la delegación de cierre para el turno abierto.
        /// </summary>
        public async Task HandleAsync(CodigoCaja codigoCaja, ResponsableCaja nuevoResponsable)
        {
            // 1. Obtener turno abierto o lanzar
            var turno = await _repo.GetTurnoAbiertoAsync(codigoCaja)
                        ?? throw new InvalidOperationException(
                               $"No existe un turno abierto para la caja {codigoCaja.Value}.");

            // 2. Delegar el cierre
            turno.DelegarCierre(nuevoResponsable);

            // 3. Persistir cambio y confirmar
            await _repo.UpdateTurnoCajaAsync(turno);
            await _uow.CommitAsync();
        }
    }
}
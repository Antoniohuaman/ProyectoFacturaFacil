using System.Threading.Tasks;
using ControlCajaBC.Application.Interfaces;
using ControlCajaBC.Domain.Aggregates;
using ControlCajaBC.Domain.Entities;
using ControlCajaBC.Domain.ValueObjects;

namespace ControlCajaBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso: abrir un nuevo turno de caja.
    /// </summary>
    public class AperturaTurnoUseCase
    {
        private readonly IControlCajaRepository _repo;
        private readonly IUnitOfWork _uow;

        public AperturaTurnoUseCase(IControlCajaRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow  = uow;
        }

        /// <summary>
        /// Ejecuta la apertura de turno con el código proporcionado.
        /// </summary>
        public async Task HandleAsync(
            CodigoCaja       codigo,
            FechaHora        fechaApertura,
            ResponsableCaja  responsable,
            Monto            saldoInicial)
        {
            // Construyo el agregado con el código que me pasan
            var turno = new TurnoCaja(
                codigo,
                fechaApertura,
                responsable,
                saldoInicial);

            // Persisto y confirmo
            await _repo.AddTurnoCajaAsync(turno);
            await _uow.CommitAsync();
        }
    }
}

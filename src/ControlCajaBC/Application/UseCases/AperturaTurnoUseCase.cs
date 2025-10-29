using System.Threading.Tasks;
using ControlCajaBC.Application.Interfaces;
using ControlCajaBC.Domain.Aggregates;
using ControlCajaBC.Domain.Entities;
using ControlCajaBC.Domain.ValueObjects;
using SharedKernel.Application.Interfaces; // ITenantContext
using SharedKernel.ValueObjects;            // EmpresaId, EstablecimientoId

namespace ControlCajaBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso: abrir un nuevo turno de caja.
    /// </summary>
    public class AperturaTurnoUseCase
    {
        private readonly IControlCajaRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;

        public AperturaTurnoUseCase(IControlCajaRepository repo, IUnitOfWork uow, ITenantContext tenant)
        {
            _repo = repo;
            _uow  = uow;
            _tenant = tenant ?? throw new System.ArgumentNullException(nameof(tenant));
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
            // Derivación de EmpresaId/EstablecimientoId: se obtiene de ITenantContext.
            // Nota: EstablecimientoId no está disponible en el contexto actual de este BC; se pasa null (no aplica por ahora).
            var empresaId = _tenant.EmpresaId ?? throw new System.InvalidOperationException("EmpresaId del contexto es obligatorio.");
            EstablecimientoId? establecimientoId = null; // ver nota arriba

            var turno = new TurnoCaja(
                codigo,
                empresaId,
                establecimientoId,
                fechaApertura,
                responsable,
                saldoInicial);

            // Persisto y confirmo
            await _repo.AddTurnoCajaAsync(turno);
            await _uow.CommitAsync();
        }
    }
}

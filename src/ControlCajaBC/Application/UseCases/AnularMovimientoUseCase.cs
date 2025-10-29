using System;
using System.Threading.Tasks;
using ControlCajaBC.Application.Interfaces;
using ControlCajaBC.Domain.Aggregates;
using ControlCajaBC.Domain.ValueObjects;
using SharedKernel.Application.Interfaces;
using SharedKernel.ValueObjects;

namespace ControlCajaBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso: anular un movimiento de un turno abierto.
    /// </summary>
    public class AnularMovimientoUseCase
    {
        private readonly IControlCajaRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;

        public AnularMovimientoUseCase(
            IControlCajaRepository repo,
            IUnitOfWork uow,
            ITenantContext tenant)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow  = uow  ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        /// <summary>
        /// Ejecuta la anulación de un movimiento identificado por su Id.
        /// </summary>
        public async Task HandleAsync(
            CodigoCaja codigoCaja,
            Guid       movimientoId)
        {
         // 1. Obtener turno abierto dentro del contexto de empresa
         var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");
         EstablecimientoId? establecimientoId = null;
         var turno = await _repo.GetTurnoAbiertoAsync(codigoCaja, empresaId, establecimientoId)
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

using System;
using System.Threading.Tasks;
using ControlCajaBC.Application.Interfaces;
using ControlCajaBC.Domain.Aggregates;
using ControlCajaBC.Domain.Entities;
using ControlCajaBC.Domain.ValueObjects;
using SharedKernel.Application.Interfaces;
using SharedKernel.ValueObjects;

namespace ControlCajaBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso: registrar un ingreso o egreso en un turno abierto.
    /// </summary>
    public class RegistrarMovimientoUseCase
    {
        private readonly IControlCajaRepository _repo;
        private readonly IUnitOfWork            _uow;
        private readonly ITenantContext _tenant;

        public RegistrarMovimientoUseCase(IControlCajaRepository repo, IUnitOfWork uow, ITenantContext tenant)
        {
            _repo = repo;
            _uow  = uow;
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
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
         // Derivación de EmpresaId/EstablecimientoId desde el contexto actual.
         var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");
         EstablecimientoId? establecimientoId = null; // Establecimiento no disponible en contexto de este BC

         var turno = await _repo.GetTurnoAbiertoAsync(codigoCaja, empresaId, establecimientoId)
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

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
    /// Caso de uso: ajustar el saldo inicial de un turno abierto.
    /// </summary>
    public class AjustarSaldoUseCase
    {
        private readonly IControlCajaRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;

        public AjustarSaldoUseCase(IControlCajaRepository repo, IUnitOfWork uow, ITenantContext tenant)
        {
            _repo = repo;
            _uow  = uow;
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        /// <summary>
        /// Ejecuta el ajuste de saldo.
        /// </summary>
        public async Task HandleAsync(CodigoCaja codigoCaja, Monto nuevoSaldo)
        {
            // 1. Obtener turno abierto o error
         var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");
         EstablecimientoId? establecimientoId = null;
         var turno = await _repo.GetTurnoAbiertoAsync(codigoCaja, empresaId, establecimientoId)
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

using System;
using System.Threading.Tasks;
using ControlCajaBC.Application.Interfaces;
using ControlCajaBC.Domain.ValueObjects;
using ControlCajaBC.Domain.Entities;
using SharedKernel.Application.Interfaces;
using SharedKernel.ValueObjects;

namespace ControlCajaBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso: delegar el cierre del turno a otro responsable.
    /// </summary>
    public class DelegarCierreUseCase
    {
    private readonly IControlCajaRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly ITenantContext _tenant;

        public DelegarCierreUseCase(IControlCajaRepository repo, IUnitOfWork uow, ITenantContext tenant)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow  = uow  ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        /// <summary>
        /// Ejecuta la delegación de cierre para el turno abierto.
        /// </summary>
        public async Task HandleAsync(CodigoCaja codigoCaja, ResponsableCaja nuevoResponsable)
        {
            // 1. Obtener turno abierto o lanzar
         var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");
         EstablecimientoId? establecimientoId = null;
         var turno = await _repo.GetTurnoAbiertoAsync(codigoCaja, empresaId, establecimientoId)
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
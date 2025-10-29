using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ControlCajaBC.Application.Interfaces;
using ControlCajaBC.Domain.ValueObjects;
using SharedKernel.Application.Interfaces;
using SharedKernel.ValueObjects;

namespace ControlCajaBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso: consulta el historial de movimientos de un turno.
    /// </summary>
    public class ConsultarHistorialUseCase
    {
        private readonly IControlCajaRepository _repo;
        private readonly ITenantContext _tenant;
        public ConsultarHistorialUseCase(IControlCajaRepository repo, ITenantContext tenant)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        /// <summary>
        /// Ejecuta la consulta de movimientos para la caja indicada.
        /// </summary>
        public async Task<IReadOnlyCollection<MovimientoDto>> HandleAsync(CodigoCaja codigoCaja)
        {
            // 1. Asegurar que exista un turno abierto
         var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");
         EstablecimientoId? establecimientoId = null;
         var turno = await _repo.GetTurnoAbiertoAsync(codigoCaja, empresaId, establecimientoId)
                        ?? throw new InvalidOperationException(
                               $"No existe un turno abierto para la caja {codigoCaja.Value}.");

            // 2. Obtener movimientos (usando el repo)
            var movimientos = await _repo.GetMovimientosAsync(codigoCaja, empresaId, establecimientoId);

            // 3. Mapear a DTO
            var dtos = movimientos
                       .Select(m => new MovimientoDto(
                            m.Id,
                            m.FechaHora.Value,
                            m.Monto.Value,
                            m.Tipo))
                       .ToList()
                       .AsReadOnly();

            return dtos;
        }
    }
}

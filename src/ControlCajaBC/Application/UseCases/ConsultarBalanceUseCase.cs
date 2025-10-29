// src/ControlCajaBC/Application/UseCases/ConsultarBalanceUseCase.cs

using System;
using System.Threading.Tasks;
using ControlCajaBC.Application.Interfaces;
using ControlCajaBC.Domain.ValueObjects;
using SharedKernel.Application.Interfaces;
using SharedKernel.ValueObjects;

namespace ControlCajaBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso: consulta el saldo actual y la diferencia respecto al saldo inicial.
    /// </summary>
    public class ConsultarBalanceUseCase
    {
        private readonly IControlCajaRepository _repo;
        private readonly ITenantContext _tenant;
        public ConsultarBalanceUseCase(IControlCajaRepository repo, ITenantContext tenant)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        /// <summary>
        /// Ejecuta la consulta de balance para la caja indicada.
        /// </summary>
        public async Task<BalanceDto> HandleAsync(CodigoCaja codigoCaja)
        {
            // 1. Obtener turno abierto o fallar
         var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");
         EstablecimientoId? establecimientoId = null; // No disponible en este contexto

         var turno = await _repo.GetTurnoAbiertoAsync(codigoCaja, empresaId, establecimientoId)
                        ?? throw new InvalidOperationException(
                               $"No existe un turno abierto para la caja {codigoCaja.Value}.");

            // 2. Calcular saldoActual y diferencia
            var saldoActual  = turno.SaldoActual.Value;
            var saldoInicial = turno.SaldoInicial.Value;
            var diferencia   = saldoActual - saldoInicial;

            // 3. Devolver DTO
            return new BalanceDto(saldoActual, diferencia);
        }
    }
}

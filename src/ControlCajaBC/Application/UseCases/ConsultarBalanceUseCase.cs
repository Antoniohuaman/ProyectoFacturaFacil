// src/ControlCajaBC/Application/UseCases/ConsultarBalanceUseCase.cs

using System;
using System.Threading.Tasks;
using ControlCajaBC.Application.Interfaces;
using ControlCajaBC.Domain.ValueObjects;

namespace ControlCajaBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso: consulta el saldo actual y la diferencia respecto al saldo inicial.
    /// </summary>
    public class ConsultarBalanceUseCase
    {
        private readonly IControlCajaRepository _repo;

        public ConsultarBalanceUseCase(IControlCajaRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        /// <summary>
        /// Ejecuta la consulta de balance para la caja indicada.
        /// </summary>
        public async Task<BalanceDto> HandleAsync(CodigoCaja codigoCaja)
        {
            // 1. Obtener turno abierto o fallar
            var turno = await _repo.GetTurnoAbiertoAsync(codigoCaja)
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

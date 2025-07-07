// src/ControlCajaBC/Application/UseCases/BalanceDto.cs

namespace ControlCajaBC.Application.UseCases
{
    /// <summary>
    /// Resultado de la consulta de balance: SaldoActual y diferencia respecto a SaldoInicial.
    /// </summary>
    public sealed class BalanceDto
    {
        public BalanceDto(decimal saldoActual, decimal diferencia)
        {
            SaldoActual = saldoActual;
            Diferencia  = diferencia;
        }

        public decimal SaldoActual { get; }
        public decimal Diferencia  { get; }
    }
}

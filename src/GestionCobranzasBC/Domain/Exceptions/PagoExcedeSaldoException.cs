namespace GestionCobranzasBC.Domain.Exceptions;

/// <summary>
/// Se lanza cuando el pago supera el saldo permitido (considerando tolerancias).
/// </summary>
public sealed class PagoExcedeSaldoException : GestionCobranzasException
{
    public decimal MontoPago { get; }
    public decimal SaldoDisponible { get; }

    public PagoExcedeSaldoException(decimal montoPago, decimal saldoDisponible)
        : base($"El monto del pago ({montoPago:N2}) excede el saldo disponible ({saldoDisponible:N2}).")
    {
        MontoPago = montoPago;
        SaldoDisponible = saldoDisponible;
    }
}

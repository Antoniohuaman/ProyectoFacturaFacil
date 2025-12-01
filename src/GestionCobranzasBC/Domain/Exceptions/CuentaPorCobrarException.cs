namespace GestionCobranzasBC.Domain.Exceptions;

/// <summary>
/// Se lanza ante operaciones inválidas sobre la cuenta por cobrar.
/// </summary>
public sealed class CuentaPorCobrarException : GestionCobranzasException
{
    public CuentaPorCobrarException(string message)
        : base(message)
    {
    }

    public static CuentaPorCobrarException YaCancelada()
        => new("La cuenta por cobrar ya se encuentra cancelada.");

    public static CuentaPorCobrarException NoAdmitePagos()
        => new("La cuenta por cobrar no admite pagos en su estado actual.");

    public static CuentaPorCobrarException CuotaNoEncontrada(int numeroCuota)
        => new($"No se encontró la cuota número {numeroCuota} en el cronograma de crédito.");
}

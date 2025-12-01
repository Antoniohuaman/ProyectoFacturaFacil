namespace GestionCobranzasBC.Domain.Exceptions;

/// <summary>
/// Se lanza cuando una cobranza no cumple las reglas mínimas (monto, series, líneas, etc.).
/// </summary>
public sealed class CobranzaInvalidaException : GestionCobranzasException
{
    public CobranzaInvalidaException(string message)
        : base(message)
    {
    }

    public static CobranzaInvalidaException SinLineas()
        => new("La cobranza debe contener al menos una línea de pago.");

    public static CobranzaInvalidaException MontoNoValido()
        => new("El monto de la cobranza debe ser mayor que cero.");

    public static CobranzaInvalidaException SerieNoConfigurada()
        => new("No existe una serie de cobranza configurada para registrar el documento.");

    public static CobranzaInvalidaException MonedaInconsistente()
        => new("La moneda de la cobranza no coincide con la moneda de la cuenta por cobrar.");
}

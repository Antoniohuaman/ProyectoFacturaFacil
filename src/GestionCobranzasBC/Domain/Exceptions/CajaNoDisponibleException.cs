namespace GestionCobranzasBC.Domain.Exceptions;

/// <summary>
/// Se lanza cuando no es posible registrar movimientos porque la caja no está disponible.
/// </summary>
public sealed class CajaNoDisponibleException : GestionCobranzasException
{
    public CajaNoDisponibleException(string message)
        : base(message)
    {
    }

    public static CajaNoDisponibleException CajaCerrada(string nombreCaja)
        => new($"La caja '{nombreCaja}' no se encuentra abierta para registrar cobranzas.");

    public static CajaNoDisponibleException CajaNoConfigurada()
        => new("No se ha configurado una caja para el registro de cobranzas.");
}

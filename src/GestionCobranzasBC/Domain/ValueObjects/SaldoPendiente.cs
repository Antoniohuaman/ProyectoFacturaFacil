using SharedKernel.Exceptions;

namespace GestionCobranzasBC.Domain.ValueObjects;

/// <summary>
/// Modelo de saldo de una cuenta por cobrar:
/// Total emitido, monto cobrado y saldo restante.
/// </summary>
public sealed record SaldoPendiente
{
    public decimal Total { get; }
    public decimal Cobrado { get; }
    public decimal Pendiente => Total - Cobrado;

    private SaldoPendiente(decimal total, decimal cobrado)
    {
        Total = total;
        Cobrado = cobrado;
    }

    public static SaldoPendiente DesdeTotal(decimal total)
    {
        if (total < 0m)
        {
            throw new BusinessRuleException("El total de la cuenta por cobrar no puede ser negativo.");
        }

        return new SaldoPendiente(decimal.Round(total, 2), 0m);
    }

    public static SaldoPendiente Restaurar(decimal total, decimal cobrado, ToleranciaRedondeo tolerancia)
    {
        if (total < 0m || cobrado < 0m)
        {
            throw new BusinessRuleException("Los importes no pueden ser negativos.");
        }

        var diff = total - cobrado;

        if (diff < 0m && !tolerancia.EstaDentro(diff))
        {
            throw new BusinessRuleException("El monto cobrado no puede superar al total más allá de la tolerancia permitida.");
        }

        var totalRedondeado = decimal.Round(total, 2);
        var cobradoRedondeado = decimal.Round(cobrado, 2);

        if (diff < 0m && tolerancia.EstaDentro(diff))
        {
            // Se acepta sobrediferencia mínima, se ajusta a cero.
            cobradoRedondeado = totalRedondeado;
        }

        return new SaldoPendiente(totalRedondeado, cobradoRedondeado);
    }

    public SaldoPendiente AplicarCobro(decimal monto, ToleranciaRedondeo tolerancia)
    {
        if (monto <= 0m)
        {
            throw new BusinessRuleException("El monto de la cobranza debe ser mayor a cero.");
        }

        var nuevoCobrado = Cobrado + monto;
        var diff = Total - nuevoCobrado;

        if (diff < 0m && !tolerancia.EstaDentro(diff))
        {
            throw new BusinessRuleException("El monto cobrado excede el saldo permitido.");
        }

        if (diff < 0m && tolerancia.EstaDentro(diff))
        {
            // Ajuste por redondeo: consideramos la cuenta totalmente cancelada.
            nuevoCobrado = Total;
        }

        return new SaldoPendiente(Total, decimal.Round(nuevoCobrado, 2));
    }

    public bool EstaCancelado(ToleranciaRedondeo tolerancia)
        => Pendiente <= 0m || tolerancia.EstaDentro(Pendiente);
}

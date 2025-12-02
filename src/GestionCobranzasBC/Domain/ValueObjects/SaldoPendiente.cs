using System;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionCobranzasBC.Domain.ValueObjects;

/// <summary>
/// Modelo de saldo de una cuenta por cobrar expresado con <see cref="Dinero"/>.
/// Mantiene el total emitido, lo cobrado y el saldo restante respetando la tolerancia.
/// </summary>
public sealed class SaldoPendiente
{
    public Dinero Total { get; }
    public Dinero Cobrado { get; }
    public Dinero Saldo { get; }
    public ToleranciaRedondeo Tolerancia { get; }

    private SaldoPendiente(Dinero total, Dinero cobrado, Dinero saldo, ToleranciaRedondeo tolerancia)
    {
        Total = total;
        Cobrado = cobrado;
        Saldo = saldo;
        Tolerancia = tolerancia;
    }

    public static SaldoPendiente Crear(
        Dinero total,
        Dinero cobrado,
        Dinero saldo,
        ToleranciaRedondeo tolerancia)
    {
        ValidarArgumentos(total, cobrado, saldo, tolerancia);

        var saldoCalculado = total - cobrado;
        var diferenciaContraParametro = (saldoCalculado - saldo).Monto;

        if (Math.Abs(diferenciaContraParametro) > tolerancia.Valor)
        {
            throw new BusinessRuleException("El saldo informado no coincide con el total y lo cobrado dentro de la tolerancia permitida.");
        }

        var saldoNormalizado = saldoCalculado;

        if (saldoNormalizado.Monto < 0m && tolerancia.EstaDentro(saldoNormalizado.Monto))
        {
            saldoNormalizado = Dinero.Crear(0m, total.Moneda);
            cobrado = total;
        }

        return new SaldoPendiente(total, cobrado, saldoNormalizado, tolerancia);
    }

    public SaldoPendiente AplicarCobro(Dinero monto)
    {
        if (monto is null)
        {
            throw new BusinessRuleException("El monto aplicado no puede ser nulo.");
        }

        if (monto.Moneda != Total.Moneda)
        {
            throw new BusinessRuleException("La moneda del cobro debe coincidir con la de la cuenta.");
        }

        if (monto.Monto <= 0m)
        {
            throw new BusinessRuleException("El monto aplicado debe ser mayor a cero.");
        }

        var nuevoCobrado = Cobrado + monto;
        var nuevoSaldo = Total - nuevoCobrado;

        if (nuevoSaldo.Monto < 0m && !Tolerancia.EstaDentro(nuevoSaldo.Monto))
        {
            throw new BusinessRuleException("El monto cobrado excede el saldo permitido por la tolerancia.");
        }

        if (nuevoSaldo.Monto < 0m && Tolerancia.EstaDentro(nuevoSaldo.Monto))
        {
            nuevoCobrado = Total;
            nuevoSaldo = Dinero.Crear(0m, Total.Moneda);
        }

        return new SaldoPendiente(Total, nuevoCobrado, nuevoSaldo, Tolerancia);
    }

    public bool EsCancelado => Saldo.Monto <= 0m || Tolerancia.EstaDentro(Saldo.Monto);

    private static void ValidarArgumentos(
        Dinero total,
        Dinero cobrado,
        Dinero saldo,
        ToleranciaRedondeo tolerancia)
    {
        if (total is null) throw new ArgumentNullException(nameof(total));
        if (cobrado is null) throw new ArgumentNullException(nameof(cobrado));
        if (saldo is null) throw new ArgumentNullException(nameof(saldo));
        if (tolerancia is null) throw new ArgumentNullException(nameof(tolerancia));

        if (total.Moneda != cobrado.Moneda || total.Moneda != saldo.Moneda)
        {
            throw new BusinessRuleException("Todas las cantidades de dinero deben estar en la misma moneda.");
        }

        if (total.Monto < 0m)
        {
            throw new BusinessRuleException("El total de la cuenta por cobrar no puede ser negativo.");
        }

        if (cobrado.Monto < 0m)
        {
            throw new BusinessRuleException("El monto cobrado no puede ser negativo.");
        }
    }
}

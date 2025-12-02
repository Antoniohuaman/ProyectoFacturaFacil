using GestionCobranzasBC.Domain.ValueObjects;
using NUnit.Framework;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionCobranzasBC.Tests.Domain.ValueObjectsTests;

public class SaldoPendienteTests
{
    private static Dinero CrearDinero(decimal monto) => Dinero.Create(monto, Moneda.PEN());

    [Test]
    public void Crear_con_valores_validos_inicia_con_cobrado_cero()
    {
        var tolerancia = ToleranciaRedondeo.Crear(0.01m);
        var saldo = SaldoPendiente.Crear(
            CrearDinero(100m),
            CrearDinero(0m),
            CrearDinero(100m),
            tolerancia);

        Assert.That(saldo.Total.Monto, Is.EqualTo(100m));
        Assert.That(saldo.Cobrado.Monto, Is.EqualTo(0m));
        Assert.That(saldo.Saldo.Monto, Is.EqualTo(100m));
    }

    [Test]
    public void Crear_con_total_negativo_lanza_excepcion()
    {
        var tolerancia = ToleranciaRedondeo.Crear(0.01m);

        Assert.That(
            () => SaldoPendiente.Crear(
                CrearDinero(-1m),
                CrearDinero(0m),
                CrearDinero(-1m),
                tolerancia),
            Throws.TypeOf<BusinessRuleException>());
    }

    [Test]
    public void AplicarCobro_con_monto_valido_actualiza_totales()
    {
        var tolerancia = ToleranciaRedondeo.Crear(0.01m);
        var saldo = SaldoPendiente.Crear(
            CrearDinero(100m),
            CrearDinero(0m),
            CrearDinero(100m),
            tolerancia);

        var actualizado = saldo.AplicarCobro(CrearDinero(40m));

        Assert.That(actualizado.Cobrado.Monto, Is.EqualTo(40m));
        Assert.That(actualizado.Saldo.Monto, Is.EqualTo(60m));
    }

    [Test]
    public void AplicarCobro_con_monto_mayor_sin_tolerancia_lanza_excepcion()
    {
        var tolerancia = ToleranciaRedondeo.Crear(0m);
        var saldo = SaldoPendiente.Crear(
            CrearDinero(100m),
            CrearDinero(0m),
            CrearDinero(100m),
            tolerancia);

        Assert.That(
            () => saldo.AplicarCobro(CrearDinero(150m)),
            Throws.TypeOf<BusinessRuleException>());
    }

    [Test]
    public void AplicarCobro_con_sobrepago_menor_a_tolerancia_ajusta_a_cero()
    {
        var tolerancia = ToleranciaRedondeo.Crear(0.05m);
        var saldo = SaldoPendiente.Crear(
            CrearDinero(100m),
            CrearDinero(0m),
            CrearDinero(100m),
            tolerancia);

        var actualizado = saldo.AplicarCobro(CrearDinero(100.03m));

        Assert.That(actualizado.Cobrado.Monto, Is.EqualTo(100m));
        Assert.That(actualizado.Saldo.Monto, Is.EqualTo(0m));
        Assert.That(actualizado.EsCancelado, Is.True);
    }

    [Test]
    public void EsCancelado_con_saldo_dentro_de_tolerancia_retorna_true()
    {
        var tolerancia = ToleranciaRedondeo.Crear(0.05m);
        var saldo = SaldoPendiente.Crear(
            CrearDinero(100m),
            CrearDinero(99.97m),
            CrearDinero(0.03m),
            tolerancia);

        Assert.That(saldo.EsCancelado, Is.True);
    }
}

using GestionCobranzasBC.Domain.ValueObjects;
using NUnit.Framework;
using SharedKernel.Exceptions;

namespace GestionCobranzasBC.Tests.Domain.ValueObjectsTests;

public class SaldoPendienteTests
{
    [Test]
    public void DesdeTotal_conValorValido_iniciaConCobradoCero()
    {
        var saldo = SaldoPendiente.DesdeTotal(100m);

        Assert.That(saldo.Total, Is.EqualTo(100m));
        Assert.That(saldo.Cobrado, Is.EqualTo(0m));
        Assert.That(saldo.Pendiente, Is.EqualTo(100m));
    }

    [Test]
    public void DesdeTotal_conTotalNegativo_lanzaBusinessRuleException()
    {
        Assert.That(
            () => SaldoPendiente.DesdeTotal(-1m),
            Throws.TypeOf<BusinessRuleException>());
    }

    [Test]
    public void AplicarCobro_conMontoValido_actualizaCobradoYPendiente()
    {
        var saldo = SaldoPendiente.DesdeTotal(100m);
        var tolerancia = ToleranciaRedondeo.Ninguna;

        var actualizado = saldo.AplicarCobro(40m, tolerancia);

        Assert.That(actualizado.Cobrado, Is.EqualTo(40m));
        Assert.That(actualizado.Pendiente, Is.EqualTo(60m));
    }

    [Test]
    public void AplicarCobro_conMontoMayorTotalSinTolerancia_lanzaBusinessRuleException()
    {
        var saldo = SaldoPendiente.DesdeTotal(100m);
        var tolerancia = ToleranciaRedondeo.Ninguna;

        Assert.That(
            () => saldo.AplicarCobro(150m, tolerancia),
            Throws.TypeOf<BusinessRuleException>());
    }

    [Test]
    public void AplicarCobro_conSobrepagoMenorATolerancia_ajustaACancelado()
    {
        var saldo = SaldoPendiente.DesdeTotal(100m);
        var tolerancia = ToleranciaRedondeo.Desde(0.05m);

        var actualizado = saldo.AplicarCobro(100.03m, tolerancia);

        Assert.That(actualizado.Cobrado, Is.EqualTo(100m));
        Assert.That(actualizado.Pendiente, Is.EqualTo(0m));
        Assert.That(actualizado.EstaCancelado(tolerancia), Is.True);
    }

    [Test]
    public void EstaCancelado_conPendienteDentroDeTolerancia_retornaTrue()
    {
        var tolerancia = ToleranciaRedondeo.Desde(0.05m);
        var restaurado = SaldoPendiente.Restaurar(100m, 99.97m, tolerancia);

        Assert.That(restaurado.EstaCancelado(tolerancia), Is.True);
    }
}

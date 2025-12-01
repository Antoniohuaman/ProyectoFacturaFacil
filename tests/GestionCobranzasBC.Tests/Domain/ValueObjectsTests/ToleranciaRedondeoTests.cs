using GestionCobranzasBC.Domain.ValueObjects;
using NUnit.Framework;
using SharedKernel.Exceptions;

namespace GestionCobranzasBC.Tests.Domain.ValueObjectsTests;

public class ToleranciaRedondeoTests
{
    [Test]
    public void Ninguna_esCero()
    {
        var t = ToleranciaRedondeo.Ninguna;

        Assert.That(t.Valor, Is.EqualTo(0m));
    }

    [Test]
    public void Desde_conValorValido_creaInstancia()
    {
        var t = ToleranciaRedondeo.Desde(0.01m);

        Assert.That(t.Valor, Is.EqualTo(0.01m));
    }

    [Test]
    public void Desde_conValorNegativo_lanzaBusinessRuleException()
    {
        Assert.That(
            () => ToleranciaRedondeo.Desde(-0.01m),
            Throws.TypeOf<BusinessRuleException>());
    }

    [Test]
    public void Desde_conValorMayorAlMaximo_lanzaBusinessRuleException()
    {
        Assert.That(
            () => ToleranciaRedondeo.Desde(0.10m),
            Throws.TypeOf<BusinessRuleException>());
    }

    [Test]
    public void EstaDentro_conDiferenciaMenorOIgual_retornaTrue()
    {
        var tolerancia = ToleranciaRedondeo.Desde(0.01m);

        var ok = tolerancia.EstaDentro(0.005m);

        Assert.That(ok, Is.True);
    }

    [Test]
    public void EstaDentro_conDiferenciaMayor_retornaFalse()
    {
        var tolerancia = ToleranciaRedondeo.Desde(0.01m);

        var ok = tolerancia.EstaDentro(0.02m);

        Assert.That(ok, Is.False);
    }
}

using System;
using GestionCobranzasBC.Domain.ValueObjects;
using NUnit.Framework;
using SharedKernel.Exceptions;

namespace GestionCobranzasBC.Tests.Domain.ValueObjectsTests;

public class CuentaPorCobrarIdTests
{
    [Test]
    public void New_generaGuidNoVacio()
    {
        var id = CuentaPorCobrarId.New();

        Assert.That(id.Value, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void From_conGuidValido_retornaInstanciaEsperada()
    {
        var guid = Guid.NewGuid();

        var id = CuentaPorCobrarId.From(guid);

        Assert.That(id.Value, Is.EqualTo(guid));
    }

    [Test]
    public void From_conGuidVacio_lanzaBusinessRuleException()
    {
        Assert.That(
            () => CuentaPorCobrarId.From(Guid.Empty),
            Throws.TypeOf<BusinessRuleException>());
    }

    [Test]
    public void Equals_conMismoGuid_resultaIguales()
    {
        var guid = Guid.NewGuid();

        var a = CuentaPorCobrarId.From(guid);
        var b = CuentaPorCobrarId.From(guid);

        Assert.That(a, Is.EqualTo(b));
    }
}

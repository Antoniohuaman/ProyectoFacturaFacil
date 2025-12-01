using System;
using GestionCobranzasBC.Domain.ValueObjects;
using NUnit.Framework;
using SharedKernel.Exceptions;

namespace GestionCobranzasBC.Tests.Domain.ValueObjectsTests;

public class CobranzaIdTests
{
    [Test]
    public void New_siEmpaquetaGuidValido()
    {
        var id = CobranzaId.New();

        Assert.That(id.Value, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void From_conGuidValido_retornaInstancia()
    {
        var guid = Guid.NewGuid();

        var id = CobranzaId.From(guid);

        Assert.That(id.Value, Is.EqualTo(guid));
    }

    [Test]
    public void From_conGuidVacio_lanzaBusinessRuleException()
    {
        Assert.That(
            () => CobranzaId.From(Guid.Empty),
            Throws.TypeOf<BusinessRuleException>());
    }

    [Test]
    public void Equals_conMismoGuid_resultaIguales()
    {
        var guid = Guid.NewGuid();

        var a = CobranzaId.From(guid);
        var b = CobranzaId.From(guid);

        Assert.That(a, Is.EqualTo(b));
    }
}

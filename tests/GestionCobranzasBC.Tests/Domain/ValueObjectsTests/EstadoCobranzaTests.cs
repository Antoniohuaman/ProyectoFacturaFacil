using GestionCobranzasBC.Domain.ValueObjects;
using NUnit.Framework;
using SharedKernel.Exceptions;

namespace GestionCobranzasBC.Tests.Domain.ValueObjectsTests;

public class EstadoCobranzaTests
{
    [Test]
    public void DesdeCodigo_REG_retornaRegistrada()
    {
        var estado = EstadoCobranza.DesdeCodigo("reg");

        Assert.That(estado, Is.EqualTo(EstadoCobranza.Registrada));
        Assert.That(estado.EstaActiva, Is.True);
    }

    [Test]
    public void DesdeCodigo_ANU_retornaAnulada()
    {
        var estado = EstadoCobranza.DesdeCodigo("ANU");

        Assert.That(estado, Is.EqualTo(EstadoCobranza.Anulada));
        Assert.That(estado.EstaActiva, Is.False);
    }

    [Test]
    public void DesdeCodigo_desconocido_lanzaBusinessRuleException()
    {
        Assert.That(
            () => EstadoCobranza.DesdeCodigo("XYZ"),
            Throws.TypeOf<BusinessRuleException>());
    }

    [Test]
    public void Equals_conMismoCodigo_sonIguales()
    {
        var a = EstadoCobranza.DesdeCodigo("REG");
        var b = EstadoCobranza.DesdeCodigo("reg");

        Assert.That(a, Is.EqualTo(b));
    }
}

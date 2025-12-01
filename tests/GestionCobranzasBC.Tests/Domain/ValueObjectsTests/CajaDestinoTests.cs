using GestionCobranzasBC.Domain.ValueObjects;
using NUnit.Framework;
using SharedKernel.Exceptions;

namespace GestionCobranzasBC.Tests.Domain.ValueObjectsTests;

public class CajaDestinoTests
{
    [Test]
    public void Caja_creaDestinoInternoConNombreNormalizado()
    {
        var caja = CajaDestino.Caja("  CAJA PRINCIPAL  ");

        Assert.That(caja.Nombre, Is.EqualTo("CAJA PRINCIPAL"));
        Assert.That(caja.EsCajaInterna, Is.True);
    }

    [Test]
    public void Banco_creaDestinoExterno()
    {
        var banco = CajaDestino.Banco("Banco BCP");

        Assert.That(banco.Nombre, Is.EqualTo("Banco BCP"));
        Assert.That(banco.EsCajaInterna, Is.False);
    }

    [Test]
    public void Crear_conNombreVacio_lanzaBusinessRuleException()
    {
        Assert.That(
            () => CajaDestino.Caja("   "),
            Throws.TypeOf<BusinessRuleException>());
    }

    [Test]
    public void Crear_conNombreMuyLargo_lanzaBusinessRuleException()
    {
        var nombre = new string('x', 101);

        Assert.That(
            () => CajaDestino.Banco(nombre),
            Throws.TypeOf<BusinessRuleException>());
    }
}

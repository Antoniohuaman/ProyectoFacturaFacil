using GestionCobranzasBC.Domain.ValueObjects;
using NUnit.Framework;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionCobranzasBC.Tests.Domain.ValueObjectsTests;

public class DistribucionCuotaTests
{
    private static Dinero CrearDinero(decimal monto)
        => Dinero.Crear(monto, Moneda.Soles());

    [Test]
    public void Crear_conValoresValidos_retornaInstancia()
    {
        var d = DistribucionCuota.Crear(1, CrearDinero(50.123m));

        Assert.That(d.NumeroCuota, Is.EqualTo(1));
        Assert.That(d.Monto.Monto, Is.EqualTo(50.12m)); // redondeado a 2 decimales
    }

    [Test]
    public void Crear_conNumeroCuotaInvalido_lanzaBusinessRuleException()
    {
        Assert.That(
            () => DistribucionCuota.Crear(0, CrearDinero(10m)),
            Throws.TypeOf<BusinessRuleException>());
    }

    [Test]
    public void Crear_conMontoNoPositivo_lanzaBusinessRuleException()
    {
        Assert.That(
            () => DistribucionCuota.Crear(1, CrearDinero(0m)),
            Throws.TypeOf<BusinessRuleException>());
    }
}

using ListaPreciosBC.Domain.ValueObjects;
using NUnit.Framework;
using SharedKernel.Exceptions;

namespace ListaPreciosBC.Tests.Domain.ValueObjectsTests;

[TestFixture]
public class TipoReglaGlobalColumnaPrecioTests
{
    [Test]
    public void DesdeCodigo_deberia_resolver_instancia_correspondiente()
    {
        var porcentaje = TipoReglaGlobalColumnaPrecio.DesdeCodigo("PORCENTAJE");
        var montoFijo = TipoReglaGlobalColumnaPrecio.DesdeCodigo("monto_fijo");

        Assert.That(porcentaje, Is.SameAs(TipoReglaGlobalColumnaPrecio.Porcentaje));
        Assert.That(montoFijo, Is.SameAs(TipoReglaGlobalColumnaPrecio.MontoFijo));
    }

    [Test]
    public void DesdeCodigo_con_codigo_invalido_deberia_lanzar_excepcion()
    {
        Assert.That(
            () => TipoReglaGlobalColumnaPrecio.DesdeCodigo("otro"),
            Throws.TypeOf<BusinessRuleException>());
    }
}

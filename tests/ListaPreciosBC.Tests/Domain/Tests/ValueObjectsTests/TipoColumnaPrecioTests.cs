using ListaPreciosBC.Domain.ValueObjects;
using NUnit.Framework;
using SharedKernel.Exceptions;

namespace ListaPreciosBC.Tests.Domain.ValueObjectsTests;

[TestFixture]
public class TipoColumnaPrecioTests
{
    [Test]
    public void DesdeCodigo_deberia_resolver_instancia_correspondiente()
    {
        var tipoBase = TipoColumnaPrecio.DesdeCodigo("BASE");
        var tipoGlobalDesc = TipoColumnaPrecio.DesdeCodigo("GLOBAL_DESCUENTO");
        var tipoManual = TipoColumnaPrecio.DesdeCodigo("MANUAL");

        Assert.That(tipoBase, Is.SameAs(TipoColumnaPrecio.Base));
        Assert.That(tipoGlobalDesc, Is.SameAs(TipoColumnaPrecio.GlobalDescuento));
        Assert.That(tipoManual.EsManual, Is.True);
    }

    [Test]
    public void DesdeCodigo_deberia_ser_case_insensitive()
    {
        var tipo = TipoColumnaPrecio.DesdeCodigo("global_recargo");

        Assert.That(tipo, Is.SameAs(TipoColumnaPrecio.GlobalRecargo));
    }

    [Test]
    public void DesdeCodigo_con_codigo_invalido_deberia_lanzar_excepcion()
    {
        Assert.That(
            () => TipoColumnaPrecio.DesdeCodigo("NO_EXISTE"),
            Throws.TypeOf<BusinessRuleException>());
    }

    [Test]
    public void EsGlobal_deberia_ser_true_para_descuento_y_recargo()
    {
        Assert.That(TipoColumnaPrecio.GlobalDescuento.EsGlobal, Is.True);
        Assert.That(TipoColumnaPrecio.GlobalRecargo.EsGlobal, Is.True);
        Assert.That(TipoColumnaPrecio.Manual.EsGlobal, Is.False);
    }
}

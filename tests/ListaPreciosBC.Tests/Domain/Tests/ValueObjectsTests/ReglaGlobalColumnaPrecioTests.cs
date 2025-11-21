using ListaPreciosBC.Domain.ValueObjects;
using NUnit.Framework;
using SharedKernel.Exceptions;

namespace ListaPreciosBC.Tests.Domain.ValueObjectsTests;

[TestFixture]
public class ReglaGlobalColumnaPrecioTests
{
    [Test]
    public void Crear_con_porcentaje_valido_deberia_crear_instancia()
    {
        var regla = ReglaGlobalColumnaPrecio.Crear(
            TipoReglaGlobalColumnaPrecio.Porcentaje,
            10m);

        Assert.That(regla.Tipo, Is.EqualTo(TipoReglaGlobalColumnaPrecio.Porcentaje));
        Assert.That(regla.Valor, Is.EqualTo(10m));
    }

    [Test]
    public void Crear_con_monto_fijo_valido_deberia_crear_instancia()
    {
        var regla = ReglaGlobalColumnaPrecio.Crear(
            TipoReglaGlobalColumnaPrecio.MontoFijo,
            5.5m);

        Assert.That(regla.Tipo, Is.EqualTo(TipoReglaGlobalColumnaPrecio.MontoFijo));
        Assert.That(regla.Valor, Is.EqualTo(5.5m));
    }

    [Test]
    public void Crear_con_valor_negativo_deberia_lanzar_excepcion()
    {
        Assert.That(
            () => ReglaGlobalColumnaPrecio.Crear(
                TipoReglaGlobalColumnaPrecio.MontoFijo,
                -1m),
            Throws.TypeOf<BusinessRuleException>());
    }

    [Test]
    public void Crear_con_porcentaje_mayor_a_100_deberia_lanzar_excepcion()
    {
        Assert.That(
            () => ReglaGlobalColumnaPrecio.Crear(
                TipoReglaGlobalColumnaPrecio.Porcentaje,
                150m),
            Throws.TypeOf<BusinessRuleException>());
    }

    [Test]
    public void CalcularAjuste_para_porcentaje_deberia_devolver_ajuste_en_base_al_precio()
    {
        var regla = ReglaGlobalColumnaPrecio.Crear(
            TipoReglaGlobalColumnaPrecio.Porcentaje,
            10m);

        var ajuste = regla.CalcularAjuste(100m);

        Assert.That(ajuste, Is.EqualTo(10m));
    }

    [Test]
    public void CalcularAjuste_para_monto_fijo_deberia_devolver_el_mismo_valor()
    {
        var regla = ReglaGlobalColumnaPrecio.Crear(
            TipoReglaGlobalColumnaPrecio.MontoFijo,
            7.25m);

        var ajuste = regla.CalcularAjuste(100m);

        Assert.That(ajuste, Is.EqualTo(7.25m));
    }

    [Test]
    public void CalcularAjuste_con_precio_base_negativo_deberia_lanzar_excepcion()
    {
        var regla = ReglaGlobalColumnaPrecio.Crear(
            TipoReglaGlobalColumnaPrecio.MontoFijo,
            1m);

        Assert.That(
            () => regla.CalcularAjuste(-10m),
            Throws.TypeOf<BusinessRuleException>());
    }

    [Test]
    public void ConValor_deberia_crear_nueva_instancia_con_mismo_tipo_y_nuevo_valor()
    {
        var regla = ReglaGlobalColumnaPrecio.Crear(
            TipoReglaGlobalColumnaPrecio.Porcentaje,
            5m);

        var nueva = regla.ConValor(7.5m);

        Assert.That(nueva.Tipo, Is.EqualTo(regla.Tipo));
        Assert.That(nueva.Valor, Is.EqualTo(7.5m));
        Assert.That(nueva, Is.Not.SameAs(regla));
    }
}

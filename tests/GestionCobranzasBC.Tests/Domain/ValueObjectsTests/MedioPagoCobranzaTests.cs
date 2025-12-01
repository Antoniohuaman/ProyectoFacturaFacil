using System.Linq;
using GestionCobranzasBC.Domain.ValueObjects;
using NUnit.Framework;
using SharedKernel.Exceptions;

namespace GestionCobranzasBC.Tests.Domain.ValueObjectsTests;

[TestFixture]
public class MedioPagoCobranzaTests
{
    [Test]
    public void Crear_con_codigo_valido_normaliza_y_asigna_descripcion()
    {
        // Act
        var medioPago = MedioPagoCobranza.Crear("8");

        // Assert
        Assert.That(medioPago.Codigo, Is.EqualTo("008"));
        Assert.That(medioPago.Descripcion,
            Does.StartWith("Efectivo, por operaciones en las que no existe obligación de utilizar medio de pago"));
        Assert.That(medioPago.EsEfectivo, Is.True);
        Assert.That(medioPago.EsTarjeta, Is.False);
    }

    [Test]
    public void Crear_con_codigo_invalido_lanza_BusinessRuleException()
    {
        // Act & Assert
        var ex = Assert.Throws<BusinessRuleException>(() =>
        {
            _ = MedioPagoCobranza.Crear("000");
        });

        Assert.That(ex!.Message, Does.Contain("catálogo SUNAT 59"));
    }

    [Test]
    public void TryCreate_con_codigo_invalido_devuelve_false_y_out_null()
    {
        // Act
        var resultado = MedioPagoCobranza.TryCreate("000", out var medioPago);

        // Assert
        Assert.That(resultado, Is.False);
        Assert.That(medioPago, Is.Null);
    }

    [Test]
    public void TryCreate_con_codigo_valido_devuelve_true_y_instancia_correcta()
    {
        // Act
        var resultado = MedioPagoCobranza.TryCreate("005", out var medioPago);

        // Assert
        Assert.That(resultado, Is.True);
        Assert.That(medioPago, Is.Not.Null);
        Assert.That(medioPago!.Codigo, Is.EqualTo("005"));
        Assert.That(medioPago.EsTarjeta, Is.True);
    }

    [Test]
    public void ObtenerTodos_devuelve_todos_los_codigos_del_catalogo()
    {
        // Act
        var todos = MedioPagoCobranza.ObtenerTodos();

        // Assert
        // 001–013, 101–108 y 999 → 22 entradas
        Assert.That(todos.Count, Is.EqualTo(22));
        Assert.That(todos.Any(m => m.Codigo == "999"), Is.True);
    }
}

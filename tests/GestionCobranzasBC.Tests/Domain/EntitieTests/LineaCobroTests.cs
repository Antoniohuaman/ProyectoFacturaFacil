using GestionCobranzasBC.Domain.Entities;
using GestionCobranzasBC.Domain.ValueObjects;
using NUnit.Framework;
using SharedKernel.Exceptions;

namespace GestionCobranzasBC.Tests.Domain.EntitieTests;

[TestFixture]
public class LineaCobroTests
{
    private static MedioPagoCobranza CrearMedioPagoDefault()
        => MedioPagoCobranza.Crear("008"); // efectivo

    private static CajaDestino CrearCajaDestinoDefault()
        => CajaDestino.Crear("CAJA PRINCIPAL");

    [Test]
    public void Crear_con_datos_validos_inicializa_correctamente()
    {
        // Act
        var linea = LineaCobro.Crear(
            1,
            CrearMedioPagoDefault(),
            250m,
            CrearCajaDestinoDefault(),
            "OP-123");

        // Assert
        Assert.That(linea.NumeroLinea, Is.EqualTo(1));
        Assert.That(linea.Importe, Is.EqualTo(250m));
        Assert.That(linea.MedioPago.Codigo, Is.EqualTo("008"));
        Assert.That(linea.ReferenciaOperacion, Is.EqualTo("OP-123"));
        Assert.That(linea.CajaDestino, Is.Not.Null);
    }

    [Test]
    public void Crear_con_importe_no_valido_lanza_excepcion()
    {
        // Act & Assert
        var ex = Assert.Throws<BusinessRuleException>(() =>
        {
            _ = LineaCobro.Crear(
                1,
                CrearMedioPagoDefault(),
                0m,
                CrearCajaDestinoDefault(),
                null);
        });

        Assert.That(ex!.Message, Does.Contain("importe de la línea de cobro"));
    }

    [Test]
    public void Crear_con_numero_no_valido_lanza_excepcion()
    {
        // Act & Assert
        var ex = Assert.Throws<BusinessRuleException>(() =>
        {
            _ = LineaCobro.Crear(
                0,
                CrearMedioPagoDefault(),
                100m,
                CrearCajaDestinoDefault(),
                null);
        });

        Assert.That(ex!.Message, Does.Contain("número de línea"));
    }

    [Test]
    public void ActualizarImporte_con_valor_valido_actualiza_propiedad()
    {
        // Arrange
        var linea = LineaCobro.Crear(
            1,
            CrearMedioPagoDefault(),
            100m,
            CrearCajaDestinoDefault(),
            null);

        // Act
        linea.ActualizarImporte(180m);

        // Assert
        Assert.That(linea.Importe, Is.EqualTo(180m));
    }

    [Test]
    public void ActualizarImporte_con_valor_no_valido_lanza_excepcion()
    {
        // Arrange
        var linea = LineaCobro.Crear(
            1,
            CrearMedioPagoDefault(),
            100m,
            CrearCajaDestinoDefault(),
            null);

        // Act & Assert
        var ex = Assert.Throws<BusinessRuleException>(() =>
        {
            linea.ActualizarImporte(0m);
        });

        Assert.That(ex!.Message, Does.Contain("importe de la línea de cobro"));
    }

    [Test]
    public void ActualizarReferencia_normaliza_y_permited_null()
    {
        // Arrange
        var linea = LineaCobro.Crear(
            1,
            CrearMedioPagoDefault(),
            100m,
            CrearCajaDestinoDefault(),
            "   OP-1   ");

        // Act
        linea.ActualizarReferencia("  NUEVA-OP  ");

        // Assert
        Assert.That(linea.ReferenciaOperacion, Is.EqualTo("NUEVA-OP"));

        // Act 2: borrar
        linea.ActualizarReferencia("  ");

        // Assert 2
        Assert.That(linea.ReferenciaOperacion, Is.Null);
    }
}

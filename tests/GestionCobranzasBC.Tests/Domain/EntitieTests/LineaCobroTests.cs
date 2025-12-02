using GestionCobranzasBC.Domain.Entities;
using GestionCobranzasBC.Domain.ValueObjects;
using NUnit.Framework;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionCobranzasBC.Tests.Domain.EntitieTests;

[TestFixture]
public class LineaCobroTests
{
    private static MedioPagoCobranza CrearMedioPagoDefault()
        => MedioPagoCobranza.Crear("008"); // efectivo

    private static CajaDestino CrearCajaDestinoDefault()
        => CajaDestino.Crear("CAJA PRINCIPAL");

    private static Dinero CrearDinero(decimal monto)
        => Dinero.Create(monto, Moneda.PEN());

    [Test]
    public void Crear_con_datos_validos_inicializa_correctamente()
    {
        // Act
        var linea = LineaCobro.Crear(
            1,
            CrearMedioPagoDefault(),
            CrearDinero(250m),
            CrearCajaDestinoDefault(),
            "OP-123");

        // Assert
        Assert.That(linea.NumeroLinea, Is.EqualTo(1));
        Assert.That(linea.Monto.Monto, Is.EqualTo(250m));
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
                CrearDinero(0m),
                CrearCajaDestinoDefault(),
                null);
        });

        Assert.That(ex!.Message, Does.Contain("monto de la línea de cobro"));
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
                CrearDinero(100m),
                CrearCajaDestinoDefault(),
                null);
        });

        Assert.That(ex!.Message, Does.Contain("número de línea"));
    }

    [Test]
    public void ActualizarReferencia_normaliza_y_permited_null()
    {
        // Arrange
        var linea = LineaCobro.Crear(
            1,
            CrearMedioPagoDefault(),
            CrearDinero(100m),
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

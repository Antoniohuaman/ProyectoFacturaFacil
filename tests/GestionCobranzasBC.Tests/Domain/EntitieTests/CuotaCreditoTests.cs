using System;
using GestionCobranzasBC.Domain.Entities;
using NUnit.Framework;
using SharedKernel.Exceptions;

namespace GestionCobranzasBC.Tests.Domain.EntitieTests;

[TestFixture]
public class CuotaCreditoTests
{
    [Test]
    public void Crear_con_datos_validos_inicializa_correctamente()
    {
        // Arrange
        var fecha = new DateOnly(2025, 1, 15);

        // Act
        var cuota = CuotaCredito.Crear(1, fecha, 100m);

        // Assert
        Assert.That(cuota.Numero, Is.EqualTo(1));
        Assert.That(cuota.FechaVencimiento, Is.EqualTo(fecha));
        Assert.That(cuota.ImporteOriginal, Is.EqualTo(100m));
        Assert.That(cuota.MontoPagado, Is.EqualTo(0m));
        Assert.That(cuota.Saldo, Is.EqualTo(100m));
    }

    [Test]
    public void Crear_con_numero_no_valido_lanza_excepcion()
    {
        // Act & Assert
        var ex = Assert.Throws<BusinessRuleException>(() =>
        {
            _ = CuotaCredito.Crear(0, new DateOnly(2025, 1, 1), 100m);
        });

        Assert.That(ex!.Message, Does.Contain("número de cuota"));
    }

    [Test]
    public void Crear_con_importe_no_valido_lanza_excepcion()
    {
        // Act & Assert
        var ex = Assert.Throws<BusinessRuleException>(() =>
        {
            _ = CuotaCredito.Crear(1, new DateOnly(2025, 1, 1), 0m);
        });

        Assert.That(ex!.Message, Does.Contain("importe de la cuota"));
    }

    [Test]
    public void RegistrarPago_actualiza_monto_pagado_y_saldo()
    {
        // Arrange
        var cuota = CuotaCredito.Crear(1, new DateOnly(2025, 1, 1), 100m);

        // Act
        cuota.RegistrarPago(40m, 0.01m);

        // Assert
        Assert.That(cuota.MontoPagado, Is.EqualTo(40m));
        Assert.That(cuota.Saldo, Is.EqualTo(60m));
        Assert.That(cuota.EstaCancelada(0.01m), Is.False);
    }

    [Test]
    public void RegistrarPago_por_el_total_deja_saldo_cero_y_cuota_cancelada()
    {
        // Arrange
        var cuota = CuotaCredito.Crear(1, new DateOnly(2025, 1, 1), 100m);

        // Act
        cuota.RegistrarPago(100m, 0.01m);

        // Assert
        Assert.That(cuota.MontoPagado, Is.EqualTo(100m));
        Assert.That(cuota.Saldo, Is.EqualTo(0m));
        Assert.That(cuota.EstaCancelada(0.01m), Is.True);
    }

    [Test]
    public void RegistrarPago_que_excede_saldo_lanza_excepcion()
    {
        // Arrange
        var cuota = CuotaCredito.Crear(1, new DateOnly(2025, 1, 1), 100m);

        // Act & Assert
        var ex = Assert.Throws<BusinessRuleException>(() =>
        {
            cuota.RegistrarPago(150m, 0.01m);
        });

        Assert.That(ex!.Message, Does.Contain("excede el saldo permitido"));
    }
}

using System;
using GestionCobranzasBC.Domain.Entities;
using GestionCobranzasBC.Domain.ValueObjects;
using NUnit.Framework;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionCobranzasBC.Tests.Domain.EntitieTests;

[TestFixture]
public class CuotaCreditoTests
{
    private static Dinero CrearDinero(decimal monto)
        => Dinero.Create(monto, Moneda.PEN());

    [Test]
    public void Crear_con_datos_validos_inicializa_correctamente()
    {
        // Arrange
        var fecha = new DateOnly(2025, 1, 15);

        // Act
        var cuota = CuotaCredito.Crear(1, fecha, CrearDinero(100m));

        // Assert
        Assert.That(cuota.NumeroCuota, Is.EqualTo(1));
        Assert.That(cuota.FechaVencimiento, Is.EqualTo(fecha));
        Assert.That(cuota.ImporteProgramado.Monto, Is.EqualTo(100m));
        Assert.That(cuota.MontoPagado.Monto, Is.EqualTo(0m));
        Assert.That(cuota.Saldo.Monto, Is.EqualTo(100m));
    }

    [Test]
    public void Crear_con_numero_no_valido_lanza_excepcion()
    {
        // Act & Assert
        var ex = Assert.Throws<BusinessRuleException>(() =>
        {
            _ = CuotaCredito.Crear(0, new DateOnly(2025, 1, 1), CrearDinero(100m));
        });

        Assert.That(ex!.Message, Does.Contain("número de cuota"));
    }

    [Test]
    public void Crear_con_importe_no_valido_lanza_excepcion()
    {
        // Act & Assert
        var ex = Assert.Throws<BusinessRuleException>(() =>
        {
            _ = CuotaCredito.Crear(1, new DateOnly(2025, 1, 1), CrearDinero(0m));
        });

        Assert.That(ex!.Message, Does.Contain("importe programado"));
    }

    [Test]
    public void RegistrarPago_actualiza_monto_pagado_y_saldo()
    {
        // Arrange
        var cuota = CuotaCredito.Crear(1, new DateOnly(2025, 1, 1), CrearDinero(100m));
        var tolerancia = ToleranciaRedondeo.Crear(0.01m);

        // Act
        cuota.AplicarPago(CrearDinero(40m), tolerancia);

        // Assert
        Assert.That(cuota.MontoPagado.Monto, Is.EqualTo(40m));
        Assert.That(cuota.Saldo.Monto, Is.EqualTo(60m));
        Assert.That(cuota.EstaCancelada(tolerancia), Is.False);
    }

    [Test]
    public void RegistrarPago_por_el_total_deja_saldo_cero_y_cuota_cancelada()
    {
        // Arrange
        var cuota = CuotaCredito.Crear(1, new DateOnly(2025, 1, 1), CrearDinero(100m));
        var tolerancia = ToleranciaRedondeo.Crear(0.01m);

        // Act
        cuota.AplicarPago(CrearDinero(100m), tolerancia);

        // Assert
        Assert.That(cuota.MontoPagado.Monto, Is.EqualTo(100m));
        Assert.That(cuota.Saldo.Monto, Is.EqualTo(0m));
        Assert.That(cuota.EstaCancelada(tolerancia), Is.True);
    }

    [Test]
    public void RegistrarPago_que_excede_saldo_lanza_excepcion()
    {
        // Arrange
        var cuota = CuotaCredito.Crear(1, new DateOnly(2025, 1, 1), CrearDinero(100m));
        var tolerancia = ToleranciaRedondeo.Crear(0.01m);

        // Act & Assert
        var ex = Assert.Throws<BusinessRuleException>(() =>
        {
            cuota.AplicarPago(CrearDinero(150m), tolerancia);
        });

        Assert.That(ex!.Message, Does.Contain("excede el saldo permitido"));
    }
}

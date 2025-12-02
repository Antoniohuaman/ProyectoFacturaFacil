using System;
using GestionCobranzasBC.Domain.Entities;
using GestionCobranzasBC.Domain.Services;
using GestionCobranzasBC.Domain.ValueObjects;
using NUnit.Framework;
using SharedKernel.ValueObjects;

namespace GestionCobranzasBC.Tests.Domain.ServiceTests;

[TestFixture]
public class CalculadoraEstadoCuentaServiceTests
{
    private static Dinero Pen(decimal monto) => Dinero.Create(monto, Moneda.PEN());

    [Test]
    public void CalcularSaldo_agrega_importes_y_cobrados()
    {
        var cuota1 = CuotaCredito.Crear(1, DateOnly.FromDateTime(DateTime.Today.AddDays(5)), Pen(100m));
        var cuota2 = CuotaCredito.Crear(2, DateOnly.FromDateTime(DateTime.Today.AddDays(10)), Pen(200m));
        cuota2.AplicarPago(Pen(50m), ToleranciaRedondeo.Crear(0.01m));

        var service = new CalculadoraEstadoCuentaService();
        var tolerancia = ToleranciaRedondeo.Crear(0.01m);
        var saldo = service.CalcularSaldo(new[] { cuota1, cuota2 }, tolerancia);

        Assert.That(saldo.Total.Monto, Is.EqualTo(300m));
        Assert.That(saldo.Cobrado.Monto, Is.EqualTo(50m));
        Assert.That(saldo.Saldo.Monto, Is.EqualTo(250m));
    }

    [Test]
    public void CalcularEstado_con_saldo_cero_devuelve_cancelado()
    {
        var tolerancia = ToleranciaRedondeo.Crear(0.01m);
        var cuota = CuotaCredito.Crear(1, DateOnly.FromDateTime(DateTime.Today.AddDays(1)), Pen(100m));
        cuota.AplicarPago(Pen(100m), tolerancia);

        var service = new CalculadoraEstadoCuentaService();
        var saldo = service.CalcularSaldo(new[] { cuota }, tolerancia);

        var estado = service.CalcularEstado(saldo, new[] { cuota }, DateOnly.FromDateTime(DateTime.Today));
        Assert.That(estado, Is.EqualTo(EstadoCuentaPorCobrar.Cancelado));
    }

    [Test]
    public void CalcularEstado_con_pago_parcial_devuelve_parcial()
    {
        var tolerancia = ToleranciaRedondeo.Crear(0.01m);
        var cuota = CuotaCredito.Crear(1, DateOnly.FromDateTime(DateTime.Today.AddDays(5)), Pen(100m));
        cuota.AplicarPago(Pen(40m), tolerancia);

        var service = new CalculadoraEstadoCuentaService();
        var saldo = service.CalcularSaldo(new[] { cuota }, tolerancia);

        var estado = service.CalcularEstado(saldo, new[] { cuota }, DateOnly.FromDateTime(DateTime.Today));
        Assert.That(estado, Is.EqualTo(EstadoCuentaPorCobrar.Parcial));
    }

    [Test]
    public void CalcularEstado_con_cuota_vencida_con_saldo_devuelve_vencido()
    {
        var tolerancia = ToleranciaRedondeo.Crear(0.01m);
        var cuota = CuotaCredito.Crear(1, DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), Pen(100m));

        var service = new CalculadoraEstadoCuentaService();
        var saldo = service.CalcularSaldo(new[] { cuota }, tolerancia);

        var estado = service.CalcularEstado(saldo, new[] { cuota }, DateOnly.FromDateTime(DateTime.Today));
        Assert.That(estado, Is.EqualTo(EstadoCuentaPorCobrar.Vencido));
    }
}

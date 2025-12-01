using System;
using System.Linq;
using NUnit.Framework;
using GestionCobranzasBC.Domain.Aggregates;
using GestionCobranzasBC.Domain.Entities;
using GestionCobranzasBC.Domain.Events;
using GestionCobranzasBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace GestionCobranzasBC.Tests.Domain.AggregateTests;

[TestFixture]
public class CuentaPorCobrarTests
{
    private static Dinero CrearDinero(decimal monto) => Dinero.Crear(monto, Moneda.Soles());

    [Test]
    public void CrearNueva_con_datos_validos_registra_evento_Creada()
    {
        // Arrange
        var cuentaId = CuentaPorCobrarId.Crear(Guid.NewGuid());
        var clienteId = Guid.NewGuid();
        var documentoOrigen = DocumentoOrigen.Crear(
            Guid.NewGuid(),
            "FE01",
            "00000033",
            DateOnly.FromDateTime(DateTime.Today),
            Moneda.Soles());

        var cuota1 = CuotaCredito.Crear(
            1,
            DateOnly.FromDateTime(DateTime.Today.AddMonths(1)),
            CrearDinero(100m));

        var cuota2 = CuotaCredito.Crear(
            2,
            DateOnly.FromDateTime(DateTime.Today.AddMonths(2)),
            CrearDinero(150m));

        var tolerancia = ToleranciaRedondeo.Crear(0.01m);

        var saldo = SaldoPendiente.Crear(
            total: CrearDinero(250m),
            cobrado: CrearDinero(0m),
            saldo: CrearDinero(250m),
            tolerancia);

        var estado = EstadoCuentaPorCobrar.Pendiente();

        var fechaRegistro = DateOnly.FromDateTime(DateTime.Today);

        // Act
        var cuenta = CuentaPorCobrar.CrearNueva(
            cuentaId,
            documentoOrigen,
            clienteId,
            new[] { cuota1, cuota2 },
            saldo,
            estado,
            fechaRegistro,
            tolerancia);

        // Assert
        Assert.That(cuenta.Id, Is.EqualTo(cuentaId));
        Assert.That(cuenta.DocumentoOrigen.NumeroCompleto, Is.EqualTo("FE01-00000033"));
        Assert.That(cuenta.Saldo.Saldo.Monto, Is.EqualTo(250m));
        Assert.That(cuenta.Estado, Is.EqualTo(estado));

        var evento = cuenta.DomainEvents.OfType<CuentaPorCobrarCreada>().SingleOrDefault();
        Assert.That(evento, Is.Not.Null);
        Assert.That(evento!.CuentaPorCobrarId, Is.EqualTo(cuentaId));
    }

    [Test]
    public void RegistrarPagoAplicado_actualiza_estado_y_registra_eventos()
    {
        // Arrange
        var cuentaId = CuentaPorCobrarId.Crear(Guid.NewGuid());
        var clienteId = Guid.NewGuid();
        var documentoOrigen = DocumentoOrigen.Crear(
            Guid.NewGuid(),
            "FE01",
            "00000033",
            DateOnly.FromDateTime(DateTime.Today),
            Moneda.Soles());

        var cuota = CuotaCredito.Crear(
            1,
            DateOnly.FromDateTime(DateTime.Today.AddMonths(1)),
            CrearDinero(100m));

        var tolerancia = ToleranciaRedondeo.Crear(0.01m);

        var saldoInicial = SaldoPendiente.Crear(
            CrearDinero(100m),
            CrearDinero(0m),
            CrearDinero(100m),
            tolerancia);

        var estadoInicial = EstadoCuentaPorCobrar.Pendiente();
        var fechaRegistro = DateOnly.FromDateTime(DateTime.Today);

        var cuenta = CuentaPorCobrar.CrearNueva(
            cuentaId,
            documentoOrigen,
            clienteId,
            new[] { cuota },
            saldoInicial,
            estadoInicial,
            fechaRegistro,
            tolerancia);

        cuenta.LimpiarEventos(); // limpiamos evento de creación para centrarnos en el pago

        var cobranzaId = CobranzaId.Crear(Guid.NewGuid());

        var saldoDespues = SaldoPendiente.Crear(
            CrearDinero(100m),
            CrearDinero(100m),
            CrearDinero(0m),
            tolerancia);

        var estadoDespues = EstadoCuentaPorCobrar.Cancelado();
        var fechaPago = DateOnly.FromDateTime(DateTime.Today.AddDays(10));

        // Act
        cuenta.RegistrarPagoAplicado(
            cobranzaId,
            saldoDespues,
            estadoDespues,
            fechaPago);

        // Assert
        Assert.That(cuenta.Saldo.Saldo.Monto, Is.EqualTo(0m));
        Assert.That(cuenta.Estado, Is.EqualTo(estadoDespues));
        Assert.That(cuenta.FechaUltimaActualizacion, Is.EqualTo(fechaPago));

        var pagoEvento = cuenta.DomainEvents.OfType<PagoAplicadoACuota>().SingleOrDefault();
        Assert.That(pagoEvento, Is.Not.Null);
        Assert.That(pagoEvento!.CobranzaId, Is.EqualTo(cobranzaId));

        var canceladaEvento = cuenta.DomainEvents.OfType<CuentaPorCobrarCancelada>().SingleOrDefault();
        Assert.That(canceladaEvento, Is.Not.Null);
    }
}

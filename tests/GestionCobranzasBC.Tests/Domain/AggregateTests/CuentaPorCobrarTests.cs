using System;
using System.Linq;
using NUnit.Framework;
using GestionCobranzasBC.Domain.Aggregates;
using GestionCobranzasBC.Domain.Entities;
using GestionCobranzasBC.Domain.Events;
using GestionCobranzasBC.Domain.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionCobranzasBC.Tests.Domain.AggregateTests;

[TestFixture]
public class CuentaPorCobrarTests
{
    private static Dinero CrearDinero(decimal monto) => Dinero.Create(monto, Moneda.PEN());
    private static readonly TenantId Tenant = TenantId.From(Guid.Parse("11111111-2222-3333-4444-555555555555"));
    private static readonly EmpresaId Empresa = EmpresaId.From("20123456789");
    private static readonly EstablecimientoId Establecimiento = EstablecimientoId.From(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));

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
            Moneda.PEN());

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

        var estado = EstadoCuentaPorCobrar.Pendiente;

        var fechaRegistro = DateOnly.FromDateTime(DateTime.Today);

        // Act
        var cuenta = CuentaPorCobrar.CrearNueva(
            Tenant,
            Empresa,
            Establecimiento,
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
        Assert.That(cuenta.TenantId, Is.EqualTo(Tenant));
        Assert.That(cuenta.EmpresaId, Is.EqualTo(Empresa));
        Assert.That(cuenta.EstablecimientoId, Is.EqualTo(Establecimiento));
        Assert.That(cuenta.Estado, Is.EqualTo(estado));

        var evento = cuenta.DomainEvents.OfType<CuentaPorCobrarCreada>().SingleOrDefault();
        Assert.That(evento, Is.Not.Null);
        Assert.That(evento!.TenantId, Is.EqualTo(Tenant));
        Assert.That(evento.CuentaPorCobrarId, Is.EqualTo(cuentaId));
        Assert.That(evento.EmpresaId, Is.EqualTo(Empresa));
        Assert.That(evento.EstablecimientoId, Is.EqualTo(Establecimiento));
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
            Moneda.PEN());

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

        var estadoInicial = EstadoCuentaPorCobrar.Pendiente;
        var fechaRegistro = DateOnly.FromDateTime(DateTime.Today);

        var cuenta = CuentaPorCobrar.CrearNueva(
            Tenant,
            Empresa,
            Establecimiento,
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

        var estadoDespues = EstadoCuentaPorCobrar.Cancelado;
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
        Assert.That(pagoEvento!.TenantId, Is.EqualTo(Tenant));
        Assert.That(pagoEvento.CobranzaId, Is.EqualTo(cobranzaId));
        Assert.That(pagoEvento.EmpresaId, Is.EqualTo(Empresa));
        Assert.That(pagoEvento.EstablecimientoId, Is.EqualTo(Establecimiento));

        var canceladaEvento = cuenta.DomainEvents.OfType<CuentaPorCobrarCancelada>().SingleOrDefault();
        Assert.That(canceladaEvento, Is.Not.Null);
        Assert.That(canceladaEvento!.TenantId, Is.EqualTo(Tenant));
        Assert.That(canceladaEvento.EmpresaId, Is.EqualTo(Empresa));
    }

    [Test]
    public void RegistrarPagoAplicado_con_sobrepago_dentro_de_tolerancia_cancela_cuenta()
    {
        var cuenta = CrearCuentaBase(out var tolerancia);
        cuenta.LimpiarEventos();

        var saldoDespues = SaldoPendiente.Crear(
            CrearDinero(100m),
            CrearDinero(100.005m),
            CrearDinero(-0.005m),
            tolerancia);

        cuenta.RegistrarPagoAplicado(
            CobranzaId.Crear(Guid.NewGuid()),
            saldoDespues,
            EstadoCuentaPorCobrar.Cancelado,
            DateOnly.FromDateTime(DateTime.Today));

        Assert.That(cuenta.Estado.EsCancelado, Is.True);
        Assert.That(cuenta.DomainEvents.OfType<CuentaPorCobrarCancelada>().Any(), Is.True);
    }

    [Test]
    public void RegistrarPagoAplicado_con_pago_parcial_establece_estado_parcial()
    {
        var cuenta = CrearCuentaBase(out var tolerancia);
        cuenta.LimpiarEventos();

        var saldoDespues = SaldoPendiente.Crear(
            CrearDinero(100m),
            CrearDinero(40m),
            CrearDinero(60m),
            tolerancia);

        cuenta.RegistrarPagoAplicado(
            CobranzaId.Crear(Guid.NewGuid()),
            saldoDespues,
            EstadoCuentaPorCobrar.Parcial,
            DateOnly.FromDateTime(DateTime.Today));

        Assert.That(cuenta.Estado, Is.EqualTo(EstadoCuentaPorCobrar.Parcial));
        Assert.That(cuenta.DomainEvents.OfType<CuentaPorCobrarCancelada>().Any(), Is.False);
    }

    [Test]
    public void RegistrarPagoAplicado_con_pago_fuera_de_tolerancia_conserva_estado_y_lanza_excepcion()
    {
        var tolerancia = ToleranciaRedondeo.Crear(0.01m);
        var saldoInicial = SaldoPendiente.Crear(
            CrearDinero(100m),
            CrearDinero(0m),
            CrearDinero(100m),
            tolerancia);

        Assert.That(
            () => saldoInicial.AplicarCobro(CrearDinero(200m)),
            Throws.TypeOf<BusinessRuleException>());
    }

    [Test]
    public void ActualizarEstado_con_cuotas_vencidas_emite_evento_de_vencida()
    {
        var cuenta = CrearCuentaBase(out var tolerancia);
        cuenta.LimpiarEventos();

        var saldo = SaldoPendiente.Crear(
            CrearDinero(100m),
            CrearDinero(20m),
            CrearDinero(80m),
            tolerancia);

        var fechaVencida = DateOnly.FromDateTime(DateTime.Today);
        cuenta.ActualizarEstado(saldo, EstadoCuentaPorCobrar.Vencido, fechaVencida);

        var vencidaEvento = cuenta.DomainEvents.OfType<CuentaPorCobrarVencida>().SingleOrDefault();
        Assert.That(vencidaEvento, Is.Not.Null);
        Assert.That(vencidaEvento!.TenantId, Is.EqualTo(Tenant));
        Assert.That(vencidaEvento.FechaVencimiento, Is.EqualTo(fechaVencida));
    }

    private static CuentaPorCobrar CrearCuentaBase(out ToleranciaRedondeo tolerancia)
    {
        var cuentaId = CuentaPorCobrarId.Crear(Guid.NewGuid());
        var clienteId = Guid.NewGuid();
        var documentoOrigen = DocumentoOrigen.Crear(
            Guid.NewGuid(),
            "FE01",
            "00000001",
            DateOnly.FromDateTime(DateTime.Today),
            Moneda.PEN());

        var cuota = CuotaCredito.Crear(
            1,
            DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            CrearDinero(100m));

        tolerancia = ToleranciaRedondeo.Crear(0.01m);

        var saldoInicial = SaldoPendiente.Crear(
            CrearDinero(100m),
            CrearDinero(0m),
            CrearDinero(100m),
            tolerancia);

        return CuentaPorCobrar.CrearNueva(
            Tenant,
            Empresa,
            Establecimiento,
            cuentaId,
            documentoOrigen,
            clienteId,
            new[] { cuota },
            saldoInicial,
            EstadoCuentaPorCobrar.Pendiente,
            DateOnly.FromDateTime(DateTime.Today),
            tolerancia);
    }
}

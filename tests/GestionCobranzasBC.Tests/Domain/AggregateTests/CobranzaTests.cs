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
public class CobranzaTests
{
    private static Dinero CrearPen(decimal monto) => Dinero.Crear(monto, Moneda.Soles());

    [Test]
    public void CrearRegistrada_con_datos_validos_calcula_numeroCompleto_y_registra_evento()
    {
        // Arrange
        var cobranzaId = CobranzaId.Crear(Guid.NewGuid());
        var cuentaId = CuentaPorCobrarId.Crear(Guid.NewGuid());
        var fecha = DateOnly.FromDateTime(DateTime.Today);
        const string serie = "CB01";
        const int numero = 11;

        var cajaDestino = CajaDestino.CajaFisica("CAJA PRINCIPAL");

        var linea1 = LineaCobro.Crear(
            1,
            MedioPagoCobranza.DesdeCodigo("001"),
            CrearPen(1500m),
            "DEP-001",
            cajaDestino);

        var linea2 = LineaCobro.Crear(
            2,
            MedioPagoCobranza.DesdeCodigo("005"),
            CrearPen(1000m),
            "TARJ-001",
            cajaDestino);

        var distribucion1 = DistribucionCuota.Crear(1, CrearPen(1250m));
        var distribucion2 = DistribucionCuota.Crear(2, CrearPen(1250m));

        var montoTotal = CrearPen(2500m);
        var tolerancia = ToleranciaRedondeo.Crear(0.01m);

        // Act
        var cobranza = Cobranza.CrearRegistrada(
            cobranzaId,
            cuentaId,
            fecha,
            serie,
            numero,
            cajaDestino,
            new[] { linea1, linea2 },
            new[] { distribucion1, distribucion2 },
            montoTotal,
            tolerancia);

        // Assert
        Assert.That(cobranza.Id, Is.EqualTo(cobranzaId));
        Assert.That(cobranza.CuentaPorCobrarId, Is.EqualTo(cuentaId));
        Assert.That(cobranza.NumeroCompleto, Is.EqualTo("CB01-00000011"));
        Assert.That(cobranza.MontoTotal.Monto, Is.EqualTo(2500m));
        Assert.That(cobranza.LineasCobro.Count, Is.EqualTo(2));
        Assert.That(cobranza.DistribucionesCuotas.Count, Is.EqualTo(2));

        var evento = cobranza.DomainEvents.OfType<CobranzaRegistrada>().SingleOrDefault();
        Assert.That(evento, Is.Not.Null);
        Assert.That(evento!.CobranzaId, Is.EqualTo(cobranzaId));
        Assert.That(evento.NumeroCompleto, Is.EqualTo("CB01-00000011"));
    }

    [Test]
    public void CrearRegistrada_con_montos_inconsistentes_lanza_excepcion()
    {
        // Arrange
        var cobranzaId = CobranzaId.Crear(Guid.NewGuid());
        var cuentaId = CuentaPorCobrarId.Crear(Guid.NewGuid());
        var fecha = DateOnly.FromDateTime(DateTime.Today);
        const string serie = "CB01";
        const int numero = 1;

        var cajaDestino = CajaDestino.CajaFisica("CAJA PRINCIPAL");

        var linea1 = LineaCobro.Crear(
            1,
            MedioPagoCobranza.DesdeCodigo("001"),
            CrearPen(1000m),
            "DEP-001",
            cajaDestino);

        var montoTotal = CrearPen(500m); // diferente al total de líneas
        var tolerancia = ToleranciaRedondeo.Crear(0.01m);

        // Act + Assert
        Assert.That(
            () => Cobranza.CrearRegistrada(
                cobranzaId,
                cuentaId,
                fecha,
                serie,
                numero,
                cajaDestino,
                new[] { linea1 },
                Array.Empty<DistribucionCuota>(),
                montoTotal,
                tolerancia),
            Throws.TypeOf<CobranzaInvalidaException>());
    }
}

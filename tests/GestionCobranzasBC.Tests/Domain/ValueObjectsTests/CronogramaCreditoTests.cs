using System;
using System.Collections.Generic;
using GestionCobranzasBC.Domain.ValueObjects;
using NUnit.Framework;
using SharedKernel.Exceptions;

namespace GestionCobranzasBC.Tests.Domain.ValueObjectsTests;

public class CronogramaCreditoTests
{
    [Test]
    public void Crear_conCuotasValidas_creaCronograma()
    {
        var cuotas = new List<CronogramaCredito.Cuota>
        {
            new(1, new DateOnly(2025, 1, 10), 50m),
            new(2, new DateOnly(2025, 2, 10), 50m)
        };

        var cronograma = CronogramaCredito.Crear(cuotas);

        Assert.That(cronograma.Cuotas.Count, Is.EqualTo(2));
        Assert.That(cronograma.MontoTotal, Is.EqualTo(100m));
    }

    [Test]
    public void Crear_sinCuotas_lanzaBusinessRuleException()
    {
        Assert.That(
            () => CronogramaCredito.Crear(Array.Empty<CronogramaCredito.Cuota>()),
            Throws.TypeOf<BusinessRuleException>());
    }

    [Test]
    public void Crear_conCuotaImporteNoPositivo_lanzaBusinessRuleException()
    {
        var cuotas = new[]
        {
            new CronogramaCredito.Cuota(1, new DateOnly(2025, 1, 10), 0m)
        };

        Assert.That(
            () => CronogramaCredito.Crear(cuotas),
            Throws.TypeOf<BusinessRuleException>());
    }

    [Test]
    public void Crear_conCuotasNoCorrelativas_lanzaBusinessRuleException()
    {
        var cuotas = new[]
        {
            new CronogramaCredito.Cuota(1, new DateOnly(2025, 1, 10), 50m),
            new CronogramaCredito.Cuota(3, new DateOnly(2025, 2, 10), 50m)
        };

        Assert.That(
            () => CronogramaCredito.Crear(cuotas),
            Throws.TypeOf<BusinessRuleException>());
    }

    [Test]
    public void Crear_conFechasNoCrecientes_lanzaBusinessRuleException()
    {
        var cuotas = new[]
        {
            new CronogramaCredito.Cuota(1, new DateOnly(2025, 2, 10), 50m),
            new CronogramaCredito.Cuota(2, new DateOnly(2025, 1, 10), 50m)
        };

        Assert.That(
            () => CronogramaCredito.Crear(cuotas),
            Throws.TypeOf<BusinessRuleException>());
    }
}

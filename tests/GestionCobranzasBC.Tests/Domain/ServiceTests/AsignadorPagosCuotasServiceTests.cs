using System;
using System.Linq;
using GestionCobranzasBC.Domain.Entities;
using GestionCobranzasBC.Domain.Policies;
using GestionCobranzasBC.Domain.Services;
using GestionCobranzasBC.Domain.ValueObjects;
using NUnit.Framework;
using SharedKernel.ValueObjects;

namespace GestionCobranzasBC.Tests.Domain.ServiceTests;

[TestFixture]
public class AsignadorPagosCuotasServiceTests
{
    private static Dinero Pen(decimal monto) => Dinero.Create(monto, Moneda.PEN());

    [Test]
    public void AplicarDistribucionPago_actualiza_montos_y_respeta_orden_de_politica()
    {
        var tolerancia = ToleranciaRedondeo.Crear(0.01m);
        var cuota1 = CuotaCredito.Crear(1, DateOnly.FromDateTime(DateTime.Today.AddDays(30)), Pen(100m));
        var cuota2 = CuotaCredito.Crear(2, DateOnly.FromDateTime(DateTime.Today.AddDays(10)), Pen(200m));

        var service = new AsignadorPagosCuotasService(new PoliticaAplicacionPagos());

        var actualizado = service.AplicarDistribucionPago(
            new[] { cuota1, cuota2 },
            new[]
            {
                DistribucionCuota.Crear(2, Pen(150m)),
                DistribucionCuota.Crear(1, Pen(100m)),
            },
            tolerancia);

        var cuotaOrdenadaPrimero = actualizado.First();
        Assert.That(cuotaOrdenadaPrimero.NumeroCuota, Is.EqualTo(2));
        Assert.That(actualizado.Single(c => c.NumeroCuota == 2).MontoPagado.Monto, Is.EqualTo(150m));
        Assert.That(actualizado.Single(c => c.NumeroCuota == 1).MontoPagado.Monto, Is.EqualTo(100m));
    }
}

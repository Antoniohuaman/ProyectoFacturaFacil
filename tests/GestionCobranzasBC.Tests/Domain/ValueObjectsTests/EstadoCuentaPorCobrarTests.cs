using System;
using GestionCobranzasBC.Domain.ValueObjects;
using NUnit.Framework;
using SharedKernel.Exceptions;

namespace GestionCobranzasBC.Tests.Domain.ValueObjectsTests;

public class EstadoCuentaPorCobrarTests
{
    [TestCase("PEN", nameof(EstadoCuentaPorCobrar.Pendiente))]
    [TestCase("PAR", nameof(EstadoCuentaPorCobrar.Parcial))]
    [TestCase("VEN", nameof(EstadoCuentaPorCobrar.Vencida))]
    [TestCase("CAN", nameof(EstadoCuentaPorCobrar.Cancelada))]
    public void DesdeCodigo_conCodigosValidos_retornaInstanciaEsperada(string codigo, string propiedad)
    {
        var estado = EstadoCuentaPorCobrar.DesdeCodigo(codigo);

        var esperado = propiedad switch
        {
            nameof(EstadoCuentaPorCobrar.Pendiente) => EstadoCuentaPorCobrar.Pendiente,
            nameof(EstadoCuentaPorCobrar.Parcial) => EstadoCuentaPorCobrar.Parcial,
            nameof(EstadoCuentaPorCobrar.Vencida) => EstadoCuentaPorCobrar.Vencida,
            nameof(EstadoCuentaPorCobrar.Cancelada) => EstadoCuentaPorCobrar.Cancelada,
            _ => throw new ArgumentOutOfRangeException(nameof(propiedad))
        };

        Assert.That(estado, Is.EqualTo(esperado));
    }

    [Test]
    public void DesdeCodigo_conCodigoDesconocido_lanzaBusinessRuleException()
    {
        Assert.That(
            () => EstadoCuentaPorCobrar.DesdeCodigo("XXX"),
            Throws.TypeOf<BusinessRuleException>());
    }
}

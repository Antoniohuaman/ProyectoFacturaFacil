// tests/GestionCobranzasBC.Tests/Application/UseCases/ObtenerHistorialCobranzaUseCaseTests.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using GestionCobranzasBC.Application.DTOs;
using GestionCobranzasBC.Application.Interfaces;
using GestionCobranzasBC.Application.UseCases;
using Moq;
using NUnit.Framework;

namespace GestionCobranzasBC.Tests.Application.UseCases;

public class ObtenerHistorialCobranzaUseCaseTests
{
    [Test]
    public async Task ExecuteAsync_IdValido_DevuelveHistorial()
    {
        var cuentaId = Guid.NewGuid();

        var historialEsperado = new HistorialCobranzaDto
        {
            CuentaPorCobrarId = cuentaId,
            NumeroCuentaPorCobrar = "CXC-010"
        };

        var readModelMock = new Mock<ICobranzasReadModel>();
        readModelMock
            .Setup(r => r.ObtenerHistorialCobranzaAsync(
                cuentaId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(historialEsperado);

        var useCase = new ObtenerHistorialCobranzaUseCase(readModelMock.Object);

        var resultado = await useCase.ExecuteAsync(cuentaId);

        Assert.That(resultado, Is.SameAs(historialEsperado));

        readModelMock.Verify(r => r.ObtenerHistorialCobranzaAsync(
                cuentaId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void ExecuteAsync_IdVacio_LanzaArgumentException()
    {
        var useCase = new ObtenerHistorialCobranzaUseCase(
            new Mock<ICobranzasReadModel>().Object);

        var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await useCase.ExecuteAsync(Guid.Empty));

        Assert.That(ex!.ParamName, Is.EqualTo("cuentaPorCobrarId"));
    }
}

// tests/GestionCobranzasBC.Tests/Application/UseCases/RegistrarCobranzaInmediataUseCaseTests.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using GestionCobranzasBC.Application.DTOs;
using GestionCobranzasBC.Application.Interfaces;
using GestionCobranzasBC.Application.UseCases;
using GestionCobranzasBC.Domain.Services;
using Moq;
using NUnit.Framework;

namespace GestionCobranzasBC.Tests.Application.UseCases;

public class RegistrarCobranzaInmediataUseCaseTests
{
    [Test]
    public async Task ExecuteAsync_ComandoValido_RegistraCobranzaYConfirma()
    {
        var comando = new CobranzaDto
        {
            CuentaPorCobrarId = Guid.NewGuid(),
            MontoTotal = 100m
        };

        var esperado = new CobranzaDto
        {
            Id = Guid.NewGuid(),
            CuentaPorCobrarId = comando.CuentaPorCobrarId,
            MontoTotal = 100m
        };

        var domainServiceMock = new Mock<ICobranzasDomainService>();
        domainServiceMock
            .Setup(s => s.RegistrarCobranzaInmediataAsync(
                comando,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(esperado);

        var uowMock = new Mock<IUnitOfWorkGestionCobranzas>();

        var useCase = new RegistrarCobranzaInmediataUseCase(
            domainServiceMock.Object,
            uowMock.Object);

        var resultado = await useCase.ExecuteAsync(comando);

        Assert.That(resultado, Is.SameAs(esperado));

        domainServiceMock.Verify(s => s.RegistrarCobranzaInmediataAsync(
                comando,
                It.IsAny<CancellationToken>()),
            Times.Once);

        uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void ExecuteAsync_ComandoNull_LanzaArgumentNullException()
    {
        var useCase = new RegistrarCobranzaInmediataUseCase(
            new Mock<ICobranzasDomainService>().Object,
            new Mock<IUnitOfWorkGestionCobranzas>().Object);

        var ex = Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await useCase.ExecuteAsync(null!));

        Assert.That(ex!.ParamName, Is.EqualTo("comando"));
    }

    [Test]
    public void ExecuteAsync_CuentaPorCobrarIdVacio_LanzaArgumentException()
    {
        var comando = new CobranzaDto
        {
            CuentaPorCobrarId = Guid.Empty,
            MontoTotal = 10m
        };

        var useCase = new RegistrarCobranzaInmediataUseCase(
            new Mock<ICobranzasDomainService>().Object,
            new Mock<IUnitOfWorkGestionCobranzas>().Object);

        var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await useCase.ExecuteAsync(comando));

        Assert.That(ex!.ParamName, Is.EqualTo("comando"));
    }
}

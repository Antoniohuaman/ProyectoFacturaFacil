// tests/GestionCobranzasBC.Tests/Application/UseCases/RegistrarCobranzaPorCuotaUseCaseTests.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using GestionCobranzasBC.Application.DTOs;
using GestionCobranzasBC.Application.Interfaces;
using GestionCobranzasBC.Application.UseCases;
using Moq;
using NUnit.Framework;
using SharedKernel.ValueObjects;

namespace GestionCobranzasBC.Tests.Application.UseCases;

public class RegistrarCobranzaPorCuotaUseCaseTests
{
    [Test]
    public async Task ExecuteAsync_ComandoValido_RegistraCobranzaPorCuota()
    {
        var comando = new CobranzaDto
        {
            CuentaPorCobrarId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            EmpresaId = Guid.NewGuid(),
            EstablecimientoId = Guid.NewGuid(),
            MontoTotal = 30m
        };

        var esperado = new CobranzaDto
        {
            Id = Guid.NewGuid(),
            CuentaPorCobrarId = comando.CuentaPorCobrarId,
            MontoTotal = 30m
        };

        var domainServiceMock = new Mock<ICobranzasDomainService>();
        domainServiceMock
            .Setup(s => s.RegistrarCobranzaPorCuotaAsync(
                It.Is<TenantId>(t => t.Value == comando.TenantId),
                It.Is<EmpresaId>(e => e.Value == comando.EmpresaId.ToString()),
                It.Is<EstablecimientoId?>(e => e != null && e.Value == comando.EstablecimientoId),
                comando,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(esperado);

        var uowMock = new Mock<IUnitOfWorkGestionCobranzas>();

        var useCase = new RegistrarCobranzaPorCuotaUseCase(
            domainServiceMock.Object,
            uowMock.Object);

        var resultado = await useCase.ExecuteAsync(comando);

        Assert.That(resultado, Is.SameAs(esperado));
        domainServiceMock.Verify(s => s.RegistrarCobranzaPorCuotaAsync(
            It.Is<TenantId>(t => t.Value == comando.TenantId),
            It.Is<EmpresaId>(e => e.Value == comando.EmpresaId.ToString()),
            It.Is<EstablecimientoId?>(e => e != null && e.Value == comando.EstablecimientoId),
            comando,
            It.IsAny<CancellationToken>()),
            Times.Once);
        uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void ExecuteAsync_ComandoNull_LanzaArgumentNullException()
    {
        var useCase = new RegistrarCobranzaPorCuotaUseCase(
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
            TenantId = Guid.NewGuid(),
            EmpresaId = Guid.NewGuid(),
            MontoTotal = 30m
        };

        var useCase = new RegistrarCobranzaPorCuotaUseCase(
            new Mock<ICobranzasDomainService>().Object,
            new Mock<IUnitOfWorkGestionCobranzas>().Object);

        var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await useCase.ExecuteAsync(comando));

        Assert.That(ex!.ParamName, Is.EqualTo("comando"));
    }

    [Test]
    public void ExecuteAsync_TenantIdVacio_LanzaArgumentException()
    {
        var comando = new CobranzaDto
        {
            CuentaPorCobrarId = Guid.NewGuid(),
            TenantId = Guid.Empty,
            EmpresaId = Guid.NewGuid(),
            MontoTotal = 30m
        };

        var useCase = new RegistrarCobranzaPorCuotaUseCase(
            new Mock<ICobranzasDomainService>().Object,
            new Mock<IUnitOfWorkGestionCobranzas>().Object);

        var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await useCase.ExecuteAsync(comando));

        Assert.That(ex!.ParamName, Is.EqualTo("comando"));
    }

    [Test]
    public void ExecuteAsync_EmpresaIdVacio_LanzaArgumentException()
    {
        var comando = new CobranzaDto
        {
            CuentaPorCobrarId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            EmpresaId = Guid.Empty,
            MontoTotal = 30m
        };

        var useCase = new RegistrarCobranzaPorCuotaUseCase(
            new Mock<ICobranzasDomainService>().Object,
            new Mock<IUnitOfWorkGestionCobranzas>().Object);

        var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await useCase.ExecuteAsync(comando));

        Assert.That(ex!.ParamName, Is.EqualTo("comando"));
    }
}

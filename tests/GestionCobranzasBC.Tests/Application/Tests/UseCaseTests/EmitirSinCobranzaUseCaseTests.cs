// tests/GestionCobranzasBC.Tests/Application/UseCases/EmitirSinCobranzaUseCaseTests.cs
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

public class EmitirSinCobranzaUseCaseTests
{
    [Test]
    public async Task ExecuteAsync_LlamaServicioDominio_YConfirmaUnitOfWork()
    {
        var tenantId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        Guid? establecimientoId = Guid.NewGuid();
        var comprobanteId = Guid.NewGuid();

        var cuentaEsperada = new CuentaPorCobrarDto
        {
            Id = Guid.NewGuid(),
            NumeroCuentaPorCobrar = "CXC-0002"
        };

        var domainServiceMock = new Mock<ICobranzasDomainService>();
        domainServiceMock
            .Setup(s => s.EmitirSinCobranzaAsync(
                It.Is<TenantId>(t => t.Value == tenantId),
                It.Is<EmpresaId>(e => e.Value == empresaId.ToString()),
                It.Is<EstablecimientoId?>(e => e != null && e.Value == establecimientoId),
                comprobanteId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cuentaEsperada);

        var uowMock = new Mock<IUnitOfWorkGestionCobranzas>();

        var useCase = new EmitirSinCobranzaUseCase(
            domainServiceMock.Object,
            uowMock.Object);

        var resultado = await useCase.ExecuteAsync(
            tenantId,
            empresaId,
            establecimientoId,
            comprobanteId);

        Assert.That(resultado, Is.SameAs(cuentaEsperada));

        domainServiceMock.Verify(s => s.EmitirSinCobranzaAsync(
            It.Is<TenantId>(t => t.Value == tenantId),
            It.Is<EmpresaId>(e => e.Value == empresaId.ToString()),
            It.Is<EstablecimientoId?>(e => e != null && e.Value == establecimientoId),
            comprobanteId,
            It.IsAny<CancellationToken>()),
            Times.Once);


        uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void ExecuteAsync_TenantIdVacio_LanzaArgumentException()
    {
        var useCase = new EmitirSinCobranzaUseCase(
            new Mock<ICobranzasDomainService>().Object,
            new Mock<IUnitOfWorkGestionCobranzas>().Object);

        var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await useCase.ExecuteAsync(
                Guid.Empty,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()));

        Assert.That(ex!.ParamName, Is.EqualTo("tenantId"));
    }

    [Test]
    public void ExecuteAsync_EmpresaIdVacio_LanzaArgumentException()
    {
        var useCase = new EmitirSinCobranzaUseCase(
            new Mock<ICobranzasDomainService>().Object,
            new Mock<IUnitOfWorkGestionCobranzas>().Object);

        var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await useCase.ExecuteAsync(
                Guid.NewGuid(),
                Guid.Empty,
                Guid.NewGuid(),
                Guid.NewGuid()));

        Assert.That(ex!.ParamName, Is.EqualTo("empresaId"));
    }

    [Test]
    public void ExecuteAsync_ComprobanteIdVacio_LanzaArgumentException()
    {
        var useCase = new EmitirSinCobranzaUseCase(
            new Mock<ICobranzasDomainService>().Object,
            new Mock<IUnitOfWorkGestionCobranzas>().Object);

        var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await useCase.ExecuteAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.Empty));

        Assert.That(ex!.ParamName, Is.EqualTo("comprobanteId"));
    }
}

// tests/GestionCobranzasBC.Tests/Application/UseCases/GenerarCuentaPorCobrarDesdeComprobanteUseCaseTests.cs
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

public class GenerarCuentaPorCobrarDesdeComprobanteUseCaseTests
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
            NumeroCuentaPorCobrar = "CXC-0001"
        };

        var domainServiceMock = new Mock<ICobranzasDomainService>();
        domainServiceMock
            .Setup(s => s.GenerarCuentaPorCobrarDesdeComprobanteAsync(
                It.Is<TenantId>(t => t.Value == tenantId),
                It.Is<EmpresaId>(e => e.Value == empresaId.ToString()),
                It.Is<EstablecimientoId?>(e => e != null && e.Value == establecimientoId),
                comprobanteId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cuentaEsperada);

        var uowMock = new Mock<IUnitOfWorkGestionCobranzas>();

        var useCase = new GenerarCuentaPorCobrarDesdeComprobanteUseCase(
            domainServiceMock.Object,
            uowMock.Object);

        var resultado = await useCase.ExecuteAsync(
            tenantId,
            empresaId,
            establecimientoId,
            comprobanteId);

        Assert.That(resultado, Is.SameAs(cuentaEsperada));

        domainServiceMock.Verify(s => s.GenerarCuentaPorCobrarDesdeComprobanteAsync(
            It.Is<TenantId>(t => t.Value == tenantId),
            It.Is<EmpresaId>(e => e.Value == empresaId.ToString()),
            It.Is<EstablecimientoId?>(e => e != null && e.Value == establecimientoId),
            comprobanteId,
            It.IsAny<CancellationToken>()),
            Times.Once);

        uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void ExecuteAsync_ParametrosInvalidos_LanzaArgumentException()
    {
        var tenantId = Guid.Empty; // inválido
        var empresaId = Guid.NewGuid();
        Guid? establecimientoId = Guid.NewGuid();
        var comprobanteId = Guid.NewGuid();

        var domainServiceMock = new Mock<ICobranzasDomainService>();
        var uowMock = new Mock<IUnitOfWorkGestionCobranzas>();

        var useCase = new GenerarCuentaPorCobrarDesdeComprobanteUseCase(
            domainServiceMock.Object,
            uowMock.Object);

        var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await useCase.ExecuteAsync(tenantId, empresaId, establecimientoId, comprobanteId));

        Assert.That(ex!.ParamName, Is.EqualTo("tenantId"));
    }

    [Test]
    public async Task ExecuteAsync_SinEstablecimiento_PasaNullYConfirmaCommit()
    {
        var tenantId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        Guid? establecimientoId = null; // caso nulo
        var comprobanteId = Guid.NewGuid();

        var cuentaEsperada = new CuentaPorCobrarDto
        {
            Id = Guid.NewGuid(),
            NumeroCuentaPorCobrar = "CXC-0002"
        };

        var domainServiceMock = new Mock<ICobranzasDomainService>();
        domainServiceMock
            .Setup(s => s.GenerarCuentaPorCobrarDesdeComprobanteAsync(
                It.Is<TenantId>(t => t.Value == tenantId),
                It.Is<EmpresaId>(e => e.Value == empresaId.ToString()),
                It.Is<EstablecimientoId?>(e => e == null),
                comprobanteId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cuentaEsperada);

        var uowMock = new Mock<IUnitOfWorkGestionCobranzas>();

        var useCase = new GenerarCuentaPorCobrarDesdeComprobanteUseCase(
            domainServiceMock.Object,
            uowMock.Object);

        var resultado = await useCase.ExecuteAsync(
            tenantId,
            empresaId,
            establecimientoId,
            comprobanteId);

        Assert.That(resultado, Is.SameAs(cuentaEsperada));
        domainServiceMock.Verify(s => s.GenerarCuentaPorCobrarDesdeComprobanteAsync(
            It.Is<TenantId>(t => t.Value == tenantId),
            It.Is<EmpresaId>(e => e.Value == empresaId.ToString()),
            It.Is<EstablecimientoId?>(e => e == null),
            comprobanteId,
            It.IsAny<CancellationToken>()),
            Times.Once);

        uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

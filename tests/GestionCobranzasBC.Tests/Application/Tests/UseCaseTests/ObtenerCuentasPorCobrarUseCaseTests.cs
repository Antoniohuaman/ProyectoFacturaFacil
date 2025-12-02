// tests/GestionCobranzasBC.Tests/Application/UseCases/ObtenerCuentasPorCobrarUseCaseTests.cs
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestionCobranzasBC.Application.DTOs;
using GestionCobranzasBC.Application.Interfaces;
using GestionCobranzasBC.Application.UseCases;
using Moq;
using NUnit.Framework;

namespace GestionCobranzasBC.Tests.Application.UseCases;

public class ObtenerCuentasPorCobrarUseCaseTests
{
    [Test]
    public async Task ExecuteAsync_DevuelveCuentasDelReadModel()
    {
        var filtros = new FiltrosCobranzaDto { EmpresaId = System.Guid.NewGuid(), TenantId = System.Guid.NewGuid() };

        var listaEsperada = new List<CuentaPorCobrarDto>
        {
            new() { Id = System.Guid.NewGuid(), NumeroCuentaPorCobrar = "CXC-001" }
        };

        var readModelMock = new Mock<ICuentaPorCobrarReadModel>();
        readModelMock
            .Setup(r => r.ObtenerCuentasAsync(filtros, It.IsAny<CancellationToken>()))
            .ReturnsAsync(listaEsperada);

        var useCase = new ObtenerCuentasPorCobrarUseCase(readModelMock.Object);

        var resultado = await useCase.ExecuteAsync(filtros);

        Assert.That(resultado, Is.SameAs(listaEsperada));

        readModelMock.Verify(r => r.ObtenerCuentasAsync(
                filtros,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void ExecuteAsync_FiltrosNull_LanzaArgumentNullException()
    {
        var useCase = new ObtenerCuentasPorCobrarUseCase(
            new Mock<ICuentaPorCobrarReadModel>().Object);

        var ex = Assert.ThrowsAsync<System.ArgumentNullException>(async () =>
            await useCase.ExecuteAsync(null!));

        Assert.That(ex!.ParamName, Is.EqualTo("filtros"));
    }
}

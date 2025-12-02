// tests/GestionCobranzasBC.Tests/Application/UseCases/ListarCobranzasUseCaseTests.cs
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestionCobranzasBC.Application.DTOs;
using GestionCobranzasBC.Application.Interfaces;
using GestionCobranzasBC.Application.UseCases;
using Moq;
using NUnit.Framework;

namespace GestionCobranzasBC.Tests.Application.UseCases;

public class ListarCobranzasUseCaseTests
{
    [Test]
    public async Task ExecuteAsync_DevuelveListaDelReadModel()
    {
        var filtros = new FiltrosCobranzaDto { EmpresaId = System.Guid.NewGuid(), TenantId = System.Guid.NewGuid() };

        var listaEsperada = new List<CobranzaDto>
        {
            new() { Id = System.Guid.NewGuid(), MontoTotal = 10m },
            new() { Id = System.Guid.NewGuid(), MontoTotal = 20m }
        };

        var readModelMock = new Mock<ICobranzasReadModel>();
        readModelMock
            .Setup(r => r.ListarCobranzasAsync(filtros, It.IsAny<CancellationToken>()))
            .ReturnsAsync(listaEsperada);

        var useCase = new ListarCobranzasUseCase(readModelMock.Object);

        var resultado = await useCase.ExecuteAsync(filtros);

        Assert.That(resultado, Is.SameAs(listaEsperada));

        readModelMock.Verify(r => r.ListarCobranzasAsync(
                filtros,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void ExecuteAsync_FiltrosNull_LanzaArgumentNullException()
    {
        var useCase = new ListarCobranzasUseCase(
            new Mock<ICobranzasReadModel>().Object);

        var ex = Assert.ThrowsAsync<System.ArgumentNullException>(async () =>
            await useCase.ExecuteAsync(null!));

        Assert.That(ex!.ParamName, Is.EqualTo("filtros"));
    }
}

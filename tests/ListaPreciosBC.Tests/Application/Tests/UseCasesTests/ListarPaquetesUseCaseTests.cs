using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.DTOs;
using ListaPreciosBC.Application.UseCases;
using ListaPreciosBC.Domain.Aggregates;
using ListaPreciosBC.Domain.Repositories;
using Moq;
using NUnit.Framework;
using SharedKernel.ValueObjects;

namespace ListaPreciosBC.Tests.Application.Tests.UseCasesTests
{
    [TestFixture]
    public class ListarPaquetesUseCaseTests
    {
        private Mock<IProductoPaqueteRepository> _paqueteRepositoryMock = null!;
        private ListarPaquetesUseCase _useCase = null!;

        [SetUp]
        public void SetUp()
        {
            _paqueteRepositoryMock = new Mock<IProductoPaqueteRepository>(MockBehavior.Strict);

            _useCase = new ListarPaquetesUseCase(
                _paqueteRepositoryMock.Object);
        }

        [Test]
        public async Task EjecutarAsync_DebeRetornarListaVacia_CuandoNoHayPaquetes()
        {
            // Arrange
            var empresaId = EmpresaId.From("EMP-UNIT-TEST");

            _paqueteRepositoryMock
                .Setup(r => r.ListarPorEmpresaAsync(empresaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ProductoPaquete>());

            // Act
            var resultado = await _useCase.EjecutarAsync(empresaId, CancellationToken.None);

            // Assert
            Assert.That(resultado, Is.Not.Null);
            Assert.That(resultado, Is.Empty);

            _paqueteRepositoryMock.Verify(
                r => r.ListarPorEmpresaAsync(empresaId, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}

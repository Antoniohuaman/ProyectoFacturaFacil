using System;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.Interfaces;
using ListaPreciosBC.Application.UseCases;
using ListaPreciosBC.Domain.Aggregates;
using ListaPreciosBC.Domain.Repositories;
using Moq;
using NUnit.Framework;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace ListaPreciosBC.Tests.Application.Tests.UseCasesTests
{
    [TestFixture]
    public class EliminarPaqueteUseCaseTests
    {
        private Mock<IProductoPaqueteRepository> _paqueteRepositoryMock = null!;
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private EliminarPaqueteUseCase _useCase = null!;

        [SetUp]
        public void SetUp()
        {
            _paqueteRepositoryMock = new Mock<IProductoPaqueteRepository>(MockBehavior.Strict);
            _unitOfWorkMock = new Mock<IUnitOfWork>(MockBehavior.Strict);

            _useCase = new EliminarPaqueteUseCase(
                _paqueteRepositoryMock.Object,
                _unitOfWorkMock.Object);
        }

        [Test]
        public void EjecutarAsync_DebeLanzarNotFoundException_SiPaqueteNoExiste()
        {
            // Arrange
            var empresaId = EmpresaId.From("EMP-UNIT-TEST");
            var paqueteId = Guid.NewGuid();

            _paqueteRepositoryMock
                .Setup(r => r.ObtenerPorIdAsync(empresaId, paqueteId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ProductoPaquete?)null);

            // Act
            AsyncTestDelegate act = () =>
                _useCase.EjecutarAsync(empresaId, paqueteId, CancellationToken.None);

            // Assert
            Assert.That(act, Throws.TypeOf<NotFoundException>());

            _paqueteRepositoryMock.Verify(
                r => r.ObtenerPorIdAsync(empresaId, paqueteId, It.IsAny<CancellationToken>()),
                Times.Once);

            _paqueteRepositoryMock.Verify(
                r => r.EliminarAsync(It.IsAny<ProductoPaquete>(), It.IsAny<CancellationToken>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.CommitAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}

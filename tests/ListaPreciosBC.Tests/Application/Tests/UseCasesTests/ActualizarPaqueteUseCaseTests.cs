using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.DTOs;
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
    public class ActualizarPaqueteUseCaseTests
    {
        private Mock<IProductoPaqueteRepository> _paqueteRepositoryMock = null!;
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private ActualizarPaqueteUseCase _useCase = null!;

        [SetUp]
        public void SetUp()
        {
            _paqueteRepositoryMock = new Mock<IProductoPaqueteRepository>(MockBehavior.Strict);
            _unitOfWorkMock = new Mock<IUnitOfWork>(MockBehavior.Strict);

            _useCase = new ActualizarPaqueteUseCase(
                _paqueteRepositoryMock.Object,
                _unitOfWorkMock.Object);
        }

        [Test]
        public void EjecutarAsync_DebeLanzarArgumentNullException_SiComandoEsNull()
        {
            // Arrange
            var empresaId = EmpresaId.From("EMP-UNIT-TEST");

            // Act
            AsyncTestDelegate act = () =>
                _useCase.EjecutarAsync(empresaId, null!, CancellationToken.None);

            // Assert
            Assert.That(act, Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void EjecutarAsync_DebeLanzarNotFoundException_SiPaqueteNoExiste()
        {
            // Arrange
            var empresaId = EmpresaId.From("EMP-UNIT-TEST");
            var paqueteId = Guid.NewGuid();

            var comando = new ActualizarPaqueteDto
            {
                PaqueteId = paqueteId,
                Nombre = "PAQUETE EDITADO",
                DescuentoPorcentaje = 15m,
                Productos = Array.Empty<ProductoPaquete.LineaProductoPaquete>()
            };

            _paqueteRepositoryMock
                .Setup(r => r.ObtenerPorIdAsync(empresaId, paqueteId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ProductoPaquete?)null);

            // Act
            AsyncTestDelegate act = () =>
                _useCase.EjecutarAsync(empresaId, comando, CancellationToken.None);

            // Assert
            Assert.That(act, Throws.TypeOf<NotFoundException>());

            _paqueteRepositoryMock.Verify(
                r => r.ObtenerPorIdAsync(empresaId, paqueteId, It.IsAny<CancellationToken>()),
                Times.Once);

            _unitOfWorkMock.Verify(
                u => u.CommitAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}

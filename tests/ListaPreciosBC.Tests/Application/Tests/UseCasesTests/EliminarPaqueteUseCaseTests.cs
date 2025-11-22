using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.Interfaces;
using ListaPreciosBC.Application.UseCases;
using ListaPreciosBC.Domain.Aggregates;
using ListaPreciosBC.Domain.Events;
using ListaPreciosBC.Domain.Repositories;
using ListaPreciosBC.Domain.ValueObjects;
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

        [Test]
        public async Task EjecutarAsync_DebeEmitirEventoPaqueteEliminado()
        {
            var empresaId = EmpresaId.From("EMP-UNIT-TEST");
            var paquete = ProductoPaquete.Crear(
                empresaId,
                Guid.NewGuid(),
                NombrePaquete.Crear("Inicial"),
                PorcentajeDescuentoPaquete.Crear(5m),
                null,
                new[]
                {
                    ProductoPaquete.CrearLinea(default(ProductoId)!, default(UnidadDeMedida)!, CantidadProductoPaquete.Crear(1), 10m)
                });

            paquete.ClearDomainEvents();

            _paqueteRepositoryMock
                .Setup(r => r.ObtenerPorIdAsync(empresaId, paquete.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(paquete);

            PaqueteEliminado? eventoCapturado = null;

            _paqueteRepositoryMock
                .Setup(r => r.EliminarAsync(paquete, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Callback<ProductoPaquete, CancellationToken>((agg, _) =>
                {
                    eventoCapturado = agg.DomainEvents.OfType<PaqueteEliminado>().SingleOrDefault();
                });

            _unitOfWorkMock
                .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            await _useCase.EjecutarAsync(empresaId, paquete.Id, CancellationToken.None);

            Assert.That(eventoCapturado, Is.Not.Null);
            Assert.That(eventoCapturado!.EmpresaId, Is.EqualTo(empresaId));
            Assert.That(eventoCapturado!.PaqueteId, Is.EqualTo(paquete.Id));

            _paqueteRepositoryMock.Verify(
                r => r.EliminarAsync(paquete, It.IsAny<CancellationToken>()),
                Times.Once);

            _unitOfWorkMock.Verify(
                u => u.CommitAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}

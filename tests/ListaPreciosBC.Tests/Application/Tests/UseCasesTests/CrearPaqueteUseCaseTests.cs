using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.DTOs;
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
    public class CrearPaqueteUseCaseTests
    {
        private Mock<IProductoPaqueteRepository> _paqueteRepositoryMock = null!;
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private CrearPaqueteUseCase _useCase = null!;

        [SetUp]
        public void SetUp()
        {
            _paqueteRepositoryMock = new Mock<IProductoPaqueteRepository>(MockBehavior.Strict);
            _unitOfWorkMock = new Mock<IUnitOfWork>(MockBehavior.Strict);

            _useCase = new CrearPaqueteUseCase(
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
        public void EjecutarAsync_DebeLanzarBusinessRuleException_SiNoHayProductos()
        {
            // Arrange
            var empresaId = EmpresaId.From("EMP-UNIT-TEST");

            var comando = new CrearPaqueteDto
            {
                Nombre = "PAQUETE VACÍO",
                DescuentoPorcentaje = 10m,
                Productos = Array.Empty<ProductoPaquete.LineaProductoPaquete>()
            };

            // Act
            AsyncTestDelegate act = () =>
                _useCase.EjecutarAsync(empresaId, comando, CancellationToken.None);

            // Assert
            Assert.That(act, Throws.TypeOf<BusinessRuleException>());
        }

        [Test]
        public async Task EjecutarAsync_DebeEmitirEventoPaqueteCreado()
        {
            var empresaId = EmpresaId.From("EMP-UNIT-TEST");
            var productos = new List<ProductoPaquete.LineaProductoPaquete>
            {
                ProductoPaquete.CrearLinea(default(ProductoId)!, default(UnidadDeMedida)!, CantidadProductoPaquete.Crear(1), 25m)
            };

            PaqueteCreado? eventoCapturado = null;

            _paqueteRepositoryMock
                .Setup(r => r.GuardarAsync(It.IsAny<ProductoPaquete>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Callback<ProductoPaquete, CancellationToken>((paquete, _) =>
                {
                    eventoCapturado = paquete.DomainEvents.OfType<PaqueteCreado>().SingleOrDefault();
                });

            _unitOfWorkMock
                .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var comando = new CrearPaqueteDto
            {
                Nombre = "PAQUETE",
                Descripcion = "DESC",
                DescuentoPorcentaje = 12.5m,
                Productos = productos
            };

            var paqueteId = await _useCase.EjecutarAsync(empresaId, comando, CancellationToken.None);

            Assert.That(paqueteId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(eventoCapturado, Is.Not.Null);
            Assert.That(eventoCapturado!.EmpresaId, Is.EqualTo(empresaId));
            Assert.That(eventoCapturado!.PaqueteId, Is.EqualTo(paqueteId));

            _paqueteRepositoryMock.Verify(
                r => r.GuardarAsync(It.IsAny<ProductoPaquete>(), It.IsAny<CancellationToken>()),
                Times.Once);

            _unitOfWorkMock.Verify(
                u => u.CommitAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}

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

        [Test]
        public async Task EjecutarAsync_DebeEmitirEventoPaqueteActualizado()
        {
            var empresaId = EmpresaId.From("EMP-UNIT-TEST");
            var paquete = ProductoPaquete.Crear(
                empresaId,
                Guid.NewGuid(),
                NombrePaquete.Crear("Inicial"),
                PorcentajeDescuentoPaquete.Crear(5m),
                "desc",
                new[]
                {
                    ProductoPaquete.CrearLinea(default(ProductoId)!, default(UnidadDeMedida)!, CantidadProductoPaquete.Crear(1), 10m)
                });

            paquete.ClearDomainEvents();

            _paqueteRepositoryMock
                .Setup(r => r.ObtenerPorIdAsync(empresaId, paquete.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(paquete);

            PaqueteActualizado? eventoCapturado = null;

            _paqueteRepositoryMock
                .Setup(r => r.GuardarAsync(paquete, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Callback<ProductoPaquete, CancellationToken>((agg, _) =>
                {
                    eventoCapturado = agg.DomainEvents.OfType<PaqueteActualizado>().SingleOrDefault();
                });

            _unitOfWorkMock
                .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var comando = new ActualizarPaqueteDto
            {
                PaqueteId = paquete.Id,
                Nombre = "Editado",
                Descripcion = "nueva",
                DescuentoPorcentaje = 20m,
                Productos = new[]
                {
                    ProductoPaquete.CrearLinea(default(ProductoId)!, default(UnidadDeMedida)!, CantidadProductoPaquete.Crear(2), 15m)
                }
            };

            await _useCase.EjecutarAsync(empresaId, comando, CancellationToken.None);

            Assert.That(eventoCapturado, Is.Not.Null);
            Assert.That(eventoCapturado!.EmpresaId, Is.EqualTo(empresaId));
            Assert.That(eventoCapturado!.PaqueteId, Is.EqualTo(paquete.Id));
            Assert.That(eventoCapturado!.Nombre.Valor, Is.EqualTo("Editado"));

            _paqueteRepositoryMock.Verify(
                r => r.GuardarAsync(paquete, It.IsAny<CancellationToken>()),
                Times.Once);

            _unitOfWorkMock.Verify(
                u => u.CommitAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}

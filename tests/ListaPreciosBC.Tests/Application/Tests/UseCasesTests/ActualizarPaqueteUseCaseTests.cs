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
using SharedKernel.Application.Interfaces;
using SharedKernel.Events;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace ListaPreciosBC.Tests.Application.Tests.UseCasesTests
{
    [TestFixture]
    public class ActualizarPaqueteUseCaseTests
    {
        private Mock<IProductoPaqueteRepository> _paqueteRepositoryMock = null!;
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<ITenantContext> _tenantContextMock = null!;
        private Mock<IEventBus> _eventBusMock = null!;
        private ActualizarPaqueteUseCase _useCase = null!;
        private EmpresaId _empresaId = null!;

        [SetUp]
        public void SetUp()
        {
            _paqueteRepositoryMock = new Mock<IProductoPaqueteRepository>(MockBehavior.Strict);
            _unitOfWorkMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
            _tenantContextMock = new Mock<ITenantContext>(MockBehavior.Strict);
            _eventBusMock = new Mock<IEventBus>(MockBehavior.Strict);
            _empresaId = EmpresaId.From("EMP-UNIT-TEST");
            _tenantContextMock.SetupGet(t => t.EmpresaId).Returns(_empresaId);

            _useCase = new ActualizarPaqueteUseCase(
                _paqueteRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _tenantContextMock.Object,
                _eventBusMock.Object);
        }

        [Test]
        public void EjecutarAsync_DebeLanzarArgumentNullException_SiComandoEsNull()
        {
            // Act
            AsyncTestDelegate act = () =>
                _useCase.EjecutarAsync(null!, CancellationToken.None);

            // Assert
            Assert.That(act, Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void EjecutarAsync_DebeLanzarNotFoundException_SiPaqueteNoExiste()
        {
            // Arrange
            var paqueteId = Guid.NewGuid();

            var comando = new ActualizarPaqueteDto
            {
                PaqueteId = paqueteId,
                Nombre = "PAQUETE EDITADO",
                DescuentoPorcentaje = 15m,
                Productos = Array.Empty<PaqueteProductoLineaDto>()
            };

            _paqueteRepositoryMock
                .Setup(r => r.ObtenerPorIdAsync(_empresaId, paqueteId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ProductoPaquete?)null);

            // Act
            AsyncTestDelegate act = () =>
                _useCase.EjecutarAsync(comando, CancellationToken.None);

            // Assert
            Assert.That(act, Throws.TypeOf<NotFoundException>());

            _paqueteRepositoryMock.Verify(
                r => r.ObtenerPorIdAsync(_empresaId, paqueteId, It.IsAny<CancellationToken>()),
                Times.Once);

            _unitOfWorkMock.Verify(
                u => u.CommitAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task EjecutarAsync_DebeEmitirEventoPaqueteActualizado()
        {
            var paquete = ProductoPaquete.Crear(
                _empresaId,
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
                .Setup(r => r.ObtenerPorIdAsync(_empresaId, paquete.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(paquete);

            PaqueteActualizado? eventoCapturado = null;
            IReadOnlyCollection<IDomainEvent>? eventosPublicados = null;

            _paqueteRepositoryMock
                .Setup(r => r.GuardarAsync(paquete, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Callback<ProductoPaquete, CancellationToken>((agg, _) =>
                {
                    eventoCapturado = agg.DomainEvents.OfType<PaqueteActualizado>().SingleOrDefault();
                    var linea = agg.Productos.Single();
                    Assert.That(linea.Cantidad.Valor, Is.EqualTo(5));
                    Assert.That(linea.PrecioUnitario, Is.EqualTo(15m));
                    Assert.That(linea.UnidadDeMedida.Codigo, Is.EqualTo("NIU"));
                    Assert.That((Guid)linea.ProductoId, Is.EqualTo(Guid.Parse("bbbbbbbb-cccc-dddd-eeee-222222222222")));
                });

            _unitOfWorkMock
                .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _eventBusMock
                .Setup(b => b.PublishAsync(It.IsAny<System.Collections.Generic.IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Callback<System.Collections.Generic.IEnumerable<IDomainEvent>, CancellationToken>((eventos, _) =>
                {
                    eventosPublicados = eventos.ToList();
                });

            var comando = new ActualizarPaqueteDto
            {
                PaqueteId = paquete.Id,
                Nombre = "Editado",
                Descripcion = "nueva",
                DescuentoPorcentaje = 20m,
                Productos = new[]
                {
                    new PaqueteProductoLineaDto
                    {
                        ProductoId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-222222222222"),
                        UnidadMedidaCodigo = "NIU",
                        Cantidad = 5,
                        PrecioUnitario = 15m
                    }
                }
            };

            await _useCase.EjecutarAsync(comando, CancellationToken.None);

            Assert.That(eventoCapturado, Is.Not.Null);
            Assert.That(eventoCapturado!.EmpresaId, Is.EqualTo(_empresaId));
            Assert.That(eventoCapturado!.PaqueteId, Is.EqualTo(paquete.Id));
            Assert.That(eventoCapturado!.Nombre.Valor, Is.EqualTo("Editado"));
            Assert.That(eventosPublicados, Is.Not.Null);

            _paqueteRepositoryMock.Verify(
                r => r.GuardarAsync(paquete, It.IsAny<CancellationToken>()),
                Times.Once);

            _unitOfWorkMock.Verify(
                u => u.CommitAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            _eventBusMock.Verify(
                b => b.PublishAsync(It.IsAny<System.Collections.Generic.IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}

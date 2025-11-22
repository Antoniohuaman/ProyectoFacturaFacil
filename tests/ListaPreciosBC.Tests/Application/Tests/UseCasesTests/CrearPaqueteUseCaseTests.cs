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
    public class CrearPaqueteUseCaseTests
    {
        private Mock<IProductoPaqueteRepository> _paqueteRepositoryMock = null!;
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<ITenantContext> _tenantContextMock = null!;
        private Mock<IEventBus> _eventBusMock = null!;
        private CrearPaqueteUseCase _useCase = null!;
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

            _useCase = new CrearPaqueteUseCase(
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
        public void EjecutarAsync_DebeLanzarBusinessRuleException_SiNoHayProductos()
        {
            var comando = new CrearPaqueteDto
            {
                Nombre = "PAQUETE VACÍO",
                DescuentoPorcentaje = 10m,
                Productos = Array.Empty<PaqueteProductoLineaDto>()
            };

            // Act
            AsyncTestDelegate act = () =>
                _useCase.EjecutarAsync(comando, CancellationToken.None);

            // Assert
            Assert.That(act, Throws.TypeOf<BusinessRuleException>());
        }

        [Test]
        public async Task EjecutarAsync_DebeEmitirEventoPaqueteCreado()
        {
            var lineaDto = new PaqueteProductoLineaDto
            {
                ProductoId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-111111111111"),
                UnidadMedidaCodigo = UnidadDeMedida.NIU.Codigo,
                Cantidad = 2,
                PrecioUnitario = 25m
            };

            PaqueteCreado? eventoCapturado = null;
            IReadOnlyCollection<IDomainEvent>? eventosPublicados = null;

            _paqueteRepositoryMock
                .Setup(r => r.GuardarAsync(It.IsAny<ProductoPaquete>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Callback<ProductoPaquete, CancellationToken>((paquete, _) =>
                {
                    eventoCapturado = paquete.DomainEvents.OfType<PaqueteCreado>().SingleOrDefault();
                    Assert.That(paquete.EmpresaId, Is.EqualTo(_empresaId));
                    var linea = paquete.Productos.Single();
                    Assert.That((Guid)linea.ProductoId, Is.EqualTo(lineaDto.ProductoId));
                    Assert.That(linea.Cantidad.Valor, Is.EqualTo(lineaDto.Cantidad));
                    Assert.That(linea.PrecioUnitario, Is.EqualTo(lineaDto.PrecioUnitario));
                    Assert.That(linea.UnidadDeMedida.Codigo, Is.EqualTo(lineaDto.UnidadMedidaCodigo));
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

            var comando = new CrearPaqueteDto
            {
                Nombre = "PAQUETE",
                Descripcion = "DESC",
                DescuentoPorcentaje = 12.5m,
                Productos = new[] { lineaDto }
            };

            var paqueteId = await _useCase.EjecutarAsync(comando, CancellationToken.None);

            Assert.That(paqueteId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(eventoCapturado, Is.Not.Null);
            Assert.That(eventoCapturado!.EmpresaId, Is.EqualTo(_empresaId));
            Assert.That(eventoCapturado!.PaqueteId, Is.EqualTo(paqueteId));
            Assert.That(eventosPublicados, Is.Not.Null);
            Assert.That(eventosPublicados!.OfType<PaqueteCreado>().Count(), Is.EqualTo(1));

            _paqueteRepositoryMock.Verify(
                r => r.GuardarAsync(It.IsAny<ProductoPaquete>(), It.IsAny<CancellationToken>()),
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

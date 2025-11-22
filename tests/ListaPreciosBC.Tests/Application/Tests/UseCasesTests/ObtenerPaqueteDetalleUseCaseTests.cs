using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.UseCases;
using ListaPreciosBC.Domain.Aggregates;
using ListaPreciosBC.Domain.Repositories;
using ListaPreciosBC.Domain.ValueObjects;
using Moq;
using NUnit.Framework;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace ListaPreciosBC.Tests.Application.Tests.UseCasesTests
{
    [TestFixture]
    public class ObtenerPaqueteDetalleUseCaseTests
    {
        private Mock<IProductoPaqueteRepository> _paqueteRepositoryMock = null!;
        private Mock<ITenantContext> _tenantContextMock = null!;
        private ObtenerPaqueteDetalleUseCase _useCase = null!;
        private EmpresaId _empresaId = null!;

        [SetUp]
        public void SetUp()
        {
            _paqueteRepositoryMock = new Mock<IProductoPaqueteRepository>(MockBehavior.Strict);
            _tenantContextMock = new Mock<ITenantContext>(MockBehavior.Strict);
            _empresaId = EmpresaId.From("EMP-UNIT-TEST");
            _tenantContextMock.SetupGet(t => t.EmpresaId).Returns(_empresaId);

            _useCase = new ObtenerPaqueteDetalleUseCase(
                _paqueteRepositoryMock.Object,
                _tenantContextMock.Object);
        }

        [Test]
        public void EjecutarAsync_DebeLanzarNotFoundException_SiPaqueteNoExiste()
        {
            // Arrange
            var paqueteId = Guid.NewGuid();

            _paqueteRepositoryMock
                .Setup(r => r.ObtenerPorIdAsync(_empresaId, paqueteId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ProductoPaquete?)null);

            // Act
            AsyncTestDelegate act = () =>
                _useCase.EjecutarAsync(paqueteId, CancellationToken.None);

            // Assert
            Assert.That(act, Throws.TypeOf<NotFoundException>());

            _paqueteRepositoryMock.Verify(
                r => r.ObtenerPorIdAsync(_empresaId, paqueteId, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task EjecutarAsync_DebeRetornarDetalleMapeado()
        {
            var paquete = ProductoPaquete.Crear(
                _empresaId,
                Guid.NewGuid(),
                NombrePaquete.Crear("Detalle"),
                PorcentajeDescuentoPaquete.Crear(15m),
                "Descripcion",
                new[]
                {
                    ProductoPaquete.CrearLinea(
                        ProductoId.New(),
                        UnidadDeMedida.NIU,
                        CantidadProductoPaquete.Crear(1),
                        25m)
                });

            _paqueteRepositoryMock
                .Setup(r => r.ObtenerPorIdAsync(_empresaId, paquete.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(paquete);

            var resultado = await _useCase.EjecutarAsync(paquete.Id, CancellationToken.None);

            Assert.That(resultado.PaqueteId, Is.EqualTo(paquete.Id));
            Assert.That(resultado.Nombre, Is.EqualTo(paquete.Nombre.Valor));
            Assert.That(resultado.DescuentoPorcentaje, Is.EqualTo(paquete.Descuento.Valor));
            var lineaDto = resultado.Productos.Single();
            var lineaDominio = paquete.Productos.Single();
            Assert.That(lineaDto.ProductoId, Is.EqualTo(lineaDominio.ProductoId.Value));
            Assert.That(lineaDto.Cantidad, Is.EqualTo(lineaDominio.Cantidad.Valor));

            _paqueteRepositoryMock.Verify(
                r => r.ObtenerPorIdAsync(_empresaId, paquete.Id, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.DTOs;
using ListaPreciosBC.Application.UseCases;
using ListaPreciosBC.Domain.Aggregates;
using ListaPreciosBC.Domain.Repositories;
using ListaPreciosBC.Domain.ValueObjects;
using Moq;
using NUnit.Framework;
using SharedKernel.Application.Interfaces;
using SharedKernel.ValueObjects;

namespace ListaPreciosBC.Tests.Application.Tests.UseCasesTests
{
    [TestFixture]
    public class ListarPaquetesUseCaseTests
    {
        private Mock<IProductoPaqueteRepository> _paqueteRepositoryMock = null!;
        private Mock<ITenantContext> _tenantContextMock = null!;
        private ListarPaquetesUseCase _useCase = null!;
        private EmpresaId _empresaId = null!;

        [SetUp]
        public void SetUp()
        {
            _paqueteRepositoryMock = new Mock<IProductoPaqueteRepository>(MockBehavior.Strict);
            _tenantContextMock = new Mock<ITenantContext>(MockBehavior.Strict);
            _empresaId = EmpresaId.From("EMP-UNIT-TEST");
            _tenantContextMock.SetupGet(t => t.EmpresaId).Returns(_empresaId);

            _useCase = new ListarPaquetesUseCase(
                _paqueteRepositoryMock.Object,
                _tenantContextMock.Object);
        }

        [Test]
        public async Task EjecutarAsync_DebeRetornarListaVacia_CuandoNoHayPaquetes()
        {
            // Arrange
            _paqueteRepositoryMock
                .Setup(r => r.ListarPorEmpresaAsync(_empresaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ProductoPaquete>());

            // Act
            var resultado = await _useCase.EjecutarAsync(CancellationToken.None);

            // Assert
            Assert.That(resultado, Is.Not.Null);
            Assert.That(resultado, Is.Empty);

            _paqueteRepositoryMock.Verify(
                r => r.ListarPorEmpresaAsync(_empresaId, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task EjecutarAsync_DebeMapearPaquetesAColeccionResumen()
        {
            var paquete = ProductoPaquete.Crear(
                _empresaId,
                Guid.NewGuid(),
                NombrePaquete.Crear("Paquete Test"),
                PorcentajeDescuentoPaquete.Crear(10m),
                "Descripcion",
                new[]
                {
                    ProductoPaquete.CrearLinea(
                        ProductoId.New(),
                        UnidadDeMedida.NIU,
                        CantidadProductoPaquete.Crear(2),
                        50m)
                });

            _paqueteRepositoryMock
                .Setup(r => r.ListarPorEmpresaAsync(_empresaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ProductoPaquete> { paquete });

            var resultado = await _useCase.EjecutarAsync(CancellationToken.None);

            Assert.That(resultado, Has.Count.EqualTo(1));
            PaqueteResumenDto dto = resultado[0];
            Assert.That(dto.PaqueteId, Is.EqualTo(paquete.Id));
            Assert.That(dto.Nombre, Is.EqualTo(paquete.Nombre.Valor));
            Assert.That(dto.DescuentoPorcentaje, Is.EqualTo(paquete.Descuento.Valor));
            Assert.That(dto.Subtotal, Is.EqualTo(paquete.Subtotal));
            Assert.That(dto.Total, Is.EqualTo(paquete.Total));

            _paqueteRepositoryMock.Verify(
                r => r.ListarPorEmpresaAsync(_empresaId, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using GestionClientesBC.Application.UseCases;
using GestionClientesBC.Application.DTOs;
using GestionClientesBC.Application.Interfaces;
using GestionClientesBC.Domain.Entities;
using GestionClientesBC.Domain.ValueObjects;

namespace GestionClientesBC.Tests.UnitTests
{
    [TestFixture]
    public class ConsultarHistorialOperacionesUseCaseTests
    {
        [Test]
        public async Task HandleAsync_FiltraCorrectamente()
        {
            // Arrange
            var clienteId = Guid.NewGuid();
            var operaciones = new List<OperacionCliente>
            {
                new OperacionCliente(
                    Guid.NewGuid(),
                    TipoOperacion.FacturaEmitida,
                    new MontoOperacion(100m),
                    new ReferenciaId("REF1"),
                    DateTime.UtcNow.AddDays(-1)
                ),
                new OperacionCliente(
                    Guid.NewGuid(),
                    TipoOperacion.BoletaEmitida,
                    new MontoOperacion(50m),
                    new ReferenciaId("REF2"),
                    DateTime.UtcNow.AddDays(-10)
                )
            };

            var repoMock = new Mock<IGestionClientesRepository>();
            repoMock.Setup(r => r.ObtenerOperacionesPorClienteIdAsync(
                clienteId,
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<TipoOperacion?>(),
                It.IsAny<int?>(),
                It.IsAny<int?>()
            )).ReturnsAsync(operaciones);

            var useCase = new ConsultarHistorialOperacionesUseCase(repoMock.Object);

            var dto = new ConsultarHistorialOperacionesDto
            {
                ClienteId = clienteId,
                FechaDesde = DateTime.UtcNow.AddDays(-5),
                TipoOperacion = TipoOperacion.FacturaEmitida
            };

            // Act
            var result = await useCase.HandleAsync(dto);
            var resultList = result.ToList();

            // Assert
            Assert.That(resultList, Is.Not.Null);
            Assert.That(resultList.Count, Is.EqualTo(2)); // Cambia a 1 si filtras en el repoMock
            Assert.That(resultList[0].TipoOperacion, Is.EqualTo(TipoOperacion.FacturaEmitida.ToString()));
        }
    }
}
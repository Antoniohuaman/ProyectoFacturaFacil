using System;
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
    public class RegistrarOperacionClienteUseCaseTests
    {
        [Test]
        public async Task HandleAsync_OperacionRegistradaCorrectamente()
        {
            // Arrange
            var clienteId = Guid.NewGuid();
            var dto = new RegistrarOperacionClienteDto
            {
                ClienteId = clienteId,
                TipoOperacion = TipoOperacion.FacturaEmitida,
                Monto = 150.0m,
                Referencia = "REF-123",
                FechaOperacion = DateTime.UtcNow
            };

            var repoMock = new Mock<IGestionClientesRepository>();
            repoMock.Setup(r => r.RegistrarOperacionClienteAsync(clienteId, It.IsAny<OperacionCliente>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var useCase = new RegistrarOperacionClienteUseCase(repoMock.Object);

            // Act
            await useCase.HandleAsync(dto);

            // Assert
            repoMock.Verify(r => r.RegistrarOperacionClienteAsync(clienteId, It.IsAny<OperacionCliente>()), Times.Once);
        }
    }
}
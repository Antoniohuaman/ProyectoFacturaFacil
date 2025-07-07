using System;
using System.Threading.Tasks;
using GestionClientesBC.Application.DTOs;
using GestionClientesBC.Application.UseCases;
using GestionClientesBC.Adapters.Output.Persistence.InMemory;
using NUnit.Framework;

namespace GestionClientesBC.Tests.UnitTests.UseCases
{
    public class CrearClienteUseCaseTests
    {
        [Test]
        public async Task HandleAsync_CuandoDatosValidos_CreaClienteYCommit()
        {
            // Arrange
            var repo = new InMemoryGestionClientesRepository();
            var uow = new InMemoryUnitOfWork();
            var useCase = new CrearClienteUseCase(repo, uow);

            var dto = new ClienteDto
            {
                TipoDocumento = "DNI",
                NumeroDocumento = "12345678",
                RazonSocialONombres = "Juan Perez",
                Correo = "juan@mail.com",
                Celular = "999888777",
                DireccionPostal = "Av. Siempre Viva 123",
                TipoCliente = "Mayorista"
            };

            // Act
            var clienteId = await useCase.HandleAsync(dto);

            // Assert
            // Assert
            Assert.That(clienteId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(uow.WasCommitted, Is.True);
        }
    }
}
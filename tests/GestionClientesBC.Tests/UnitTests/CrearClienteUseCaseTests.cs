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
        public async Task HandleAsync_DatosValidos_CreaClienteYCommit()
        {
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

            var clienteId = await useCase.HandleAsync(dto);

            Assert.That(clienteId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(uow.WasCommitted, Is.True);
        }

        [TestCase(null, "12345678", "Juan Perez", TestName = "FaltaTipoDocumento")]
        [TestCase("DNI", null, "Juan Perez", TestName = "FaltaNumeroDocumento")]
        [TestCase("DNI", "12345678", null, TestName = "FaltaRazonSocialONombres")]
        public void HandleAsync_CamposObligatoriosFaltantes_LanzaArgumentException(string? tipoDoc, string? numDoc, string? nombre)
        {
            var repo = new InMemoryGestionClientesRepository();
            var uow = new InMemoryUnitOfWork();
            var useCase = new CrearClienteUseCase(repo, uow);

            var dto = new ClienteDto
            {
                TipoDocumento = tipoDoc,
                NumeroDocumento = numDoc,
                RazonSocialONombres = nombre
            };

            Assert.ThrowsAsync<ArgumentException>(async () => await useCase.HandleAsync(dto));
        }

        [Test]
        public void HandleAsync_DocumentoInvalido_LanzaArgumentException()
        {
            var repo = new InMemoryGestionClientesRepository();
            var uow = new InMemoryUnitOfWork();
            var useCase = new CrearClienteUseCase(repo, uow);

            var dto = new ClienteDto
            {
                TipoDocumento = "DNI",
                NumeroDocumento = "12A4567", // Formato inválido
                RazonSocialONombres = "Juan Perez"
            };

            Assert.ThrowsAsync<ArgumentException>(async () => await useCase.HandleAsync(dto));
        }

        [Test]
        public void HandleAsync_CorreoInvalido_LanzaArgumentException()
        {
            var repo = new InMemoryGestionClientesRepository();
            var uow = new InMemoryUnitOfWork();
            var useCase = new CrearClienteUseCase(repo, uow);

            var dto = new ClienteDto
            {
                TipoDocumento = "DNI",
                NumeroDocumento = "12345678",
                RazonSocialONombres = "Juan Perez",
                Correo = "correo-invalido"
            };

            Assert.ThrowsAsync<ArgumentException>(async () => await useCase.HandleAsync(dto));
        }

        [Test]
        public async Task HandleAsync_ClienteDuplicado_LanzaInvalidOperationException()
        {
            var repo = new InMemoryGestionClientesRepository();
            var uow = new InMemoryUnitOfWork();
            var useCase = new CrearClienteUseCase(repo, uow);

            var dto = new ClienteDto
            {
                TipoDocumento = "DNI",
                NumeroDocumento = "12345678",
                RazonSocialONombres = "Juan Perez",
                TipoCliente = "Mayorista",
            };

            await useCase.HandleAsync(dto);

            Assert.ThrowsAsync<InvalidOperationException>(async () => await useCase.HandleAsync(dto));
        }
    }
}
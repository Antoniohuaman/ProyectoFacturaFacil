using System;
using System.Threading.Tasks;
using GestionClientesBC.Application.DTOs;
using GestionClientesBC.Application.UseCases;
using GestionClientesBC.Adapters.Output.Persistence.InMemory;
using NUnit.Framework;

namespace GestionClientesBC.Tests.UnitTests.UseCases
{
    public class AutoCrearClienteDesdeComprobanteUseCaseTests
    {
        [Test]
        public async Task HandleAsync_DatosValidos_CreaClienteBasico()
        {
            var repo = new InMemoryGestionClientesRepository();
            var uow = new InMemoryUnitOfWork();
            var useCase = new AutoCrearClienteDesdeComprobanteUseCase(repo, uow);

            var dto = new AutoCrearClienteDesdeComprobanteDto
            {
                TipoDocumento = "DNI",
                NumeroDocumento = "12345678",
                RazonSocialONombres = "Cliente Factura",
                DireccionPostal = "Av. Prueba 123"
            };

            var clienteId = await useCase.HandleAsync(dto);

            Assert.That(clienteId, Is.Not.Null);
            var cliente = await repo.GetByDocumentoIdentidadAsync(
                new GestionClientesBC.Domain.ValueObjects.DocumentoIdentidad(
                    GestionClientesBC.Domain.ValueObjects.TipoDocumento.DNI, "12345678"));
            Assert.That(cliente, Is.Not.Null);
            Assert.That(cliente!.RazonSocialONombres, Is.EqualTo("Cliente Factura"));
        }

        [Test]
        public async Task HandleAsync_ClienteYaExiste_RetornaIdExistente()
        {
            var repo = new InMemoryGestionClientesRepository();
            var uow = new InMemoryUnitOfWork();
            var useCase = new AutoCrearClienteDesdeComprobanteUseCase(repo, uow);

            // Pre-crea cliente
            var dto = new AutoCrearClienteDesdeComprobanteDto
            {
                TipoDocumento = "DNI",
                NumeroDocumento = "12345678",
                RazonSocialONombres = "Cliente Existente"
            };
            var idExistente = await useCase.HandleAsync(dto);

            // Intenta crear de nuevo con mismo documento
            var idRetornado = await useCase.HandleAsync(dto);

            Assert.That(idRetornado, Is.EqualTo(idExistente));
        }

        [Test]
        public async Task HandleAsync_DocumentoInvalido_NoCreaCliente()
        {
            var repo = new InMemoryGestionClientesRepository();
            var uow = new InMemoryUnitOfWork();
            var useCase = new AutoCrearClienteDesdeComprobanteUseCase(repo, uow);

            var dto = new AutoCrearClienteDesdeComprobanteDto
            {
                TipoDocumento = "DNI",
                NumeroDocumento = "12A4567" // Inválido
            };

            var clienteId = await useCase.HandleAsync(dto);

            Assert.That(clienteId, Is.Null);
        }

        [Test]
        public async Task HandleAsync_SinNombre_AsignaClienteSinNombre()
        {
            var repo = new InMemoryGestionClientesRepository();
            var uow = new InMemoryUnitOfWork();
            var useCase = new AutoCrearClienteDesdeComprobanteUseCase(repo, uow);

            var dto = new AutoCrearClienteDesdeComprobanteDto
            {
                TipoDocumento = "DNI",
                NumeroDocumento = "87654321"
                // Sin nombre
            };

            var clienteId = await useCase.HandleAsync(dto);

            var cliente = await repo.GetByDocumentoIdentidadAsync(
                new GestionClientesBC.Domain.ValueObjects.DocumentoIdentidad(
                    GestionClientesBC.Domain.ValueObjects.TipoDocumento.DNI, "87654321"));
            Assert.That(cliente, Is.Not.Null);
            Assert.That(cliente!.RazonSocialONombres, Is.EqualTo("Cliente sin nombre"));
        }
    }
}
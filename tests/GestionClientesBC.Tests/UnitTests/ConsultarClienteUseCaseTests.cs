using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using GestionClientesBC.Application.UseCases;
using GestionClientesBC.Application.Interfaces;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Entities;
using GestionClientesBC.Domain.ValueObjects;


namespace GestionClientesBC.Tests.UnitTests
{
    [TestFixture]
    public class ConsultarClienteUseCaseTests
    {
        [Test]
        public async Task ConsultarAsync_ClienteExiste_RetornaFichaCompleta()
        {
            // Arrange
            var clienteId = Guid.NewGuid();
            var documento = new DocumentoIdentidad(TipoDocumento.DNI, "12345678");
            var cliente = new Cliente(
                clienteId,
                documento,
                "Empresa S.A.",
                "correo@empresa.com",
                "999999999",
                "Calle Falsa 123",
                TipoCliente.Mayorista,
                EstadoCliente.Activo
            );
            var contactos = new List<ContactoCliente>();
            var adjuntos = new List<AdjuntoCliente>();
            var operaciones = new List<OperacionCliente>();

            var userContextMock = new Mock<IUserContext>();

            var repoMock = new Mock<IGestionClientesRepository>();
            repoMock.Setup(x => x.GetByIdAsync(clienteId)).ReturnsAsync(cliente);
            repoMock.Setup(x => x.ObtenerContactosPorClienteIdAsync(clienteId)).ReturnsAsync(contactos);
            repoMock.Setup(x => x.ObtenerAdjuntosPorClienteIdAsync(clienteId)).ReturnsAsync(adjuntos);
            repoMock.Setup(x => x.ObtenerOperacionesPorClienteIdAsync(clienteId, It.IsAny<DateTime>())).ReturnsAsync(operaciones);

            var useCase = new ConsultarClienteUseCase(repoMock.Object, userContextMock.Object);

            // Act
            var result = await useCase.ConsultarAsync(clienteId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Cliente, Is.Not.Null);
            Assert.That(result.Contactos, Is.Not.Null);
            Assert.That(result.Adjuntos, Is.Not.Null);
            Assert.That(result.Historial, Is.Not.Null);
        }
    }
}
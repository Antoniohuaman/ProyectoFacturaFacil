using System;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using GestionClientesBC.Application.UseCases;
using GestionClientesBC.Application.DTOs;
using GestionClientesBC.Application.Interfaces;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.ValueObjects;
using GestionClientesBC.Domain.Events;

namespace GestionClientesBC.Tests.UnitTests
{
    [TestFixture]
    public class ValidarIdentidadExternaUseCaseTests
    {
        [Test]
        public async Task ValidarAsync_ActualizaClienteConDatosExternos()
        {
            // Arrange
            var cliente = new Cliente(
                Guid.NewGuid(),
                new DocumentoIdentidad(TipoDocumento.RUC, "20123456789"),
                "GENÉRICO",
                null,
                null,
                null,
                TipoCliente.Mayorista,
                EstadoCliente.Activo
            );

            var repoMock = new Mock<IGestionClientesRepository>();
            repoMock.Setup(r => r.GetByIdAsync(cliente.ClienteId)).ReturnsAsync(cliente);
            repoMock.Setup(r => r.UpdateAsync(It.IsAny<Cliente>())).Returns(Task.CompletedTask);

            var servicioMock = new Mock<IServicioIdentidadExterna>();
            servicioMock.Setup(s => s.ConsultarPorRucAsync(cliente.DocumentoIdentidad.Numero))
                .ReturnsAsync(new DatosIdentidadExterna
                {
                    RazonSocial = "EMPRESA S.A.C.",
                    DireccionFiscal = "AV. PERU 123"
                });

            var eventBusMock = new Mock<IEventBus>();
            eventBusMock.Setup(e => e.PublishAsync(It.IsAny<object>())).Returns(Task.CompletedTask);

            var useCase = new ValidarIdentidadExternaUseCase(repoMock.Object, servicioMock.Object, eventBusMock.Object);

            // Act
            await useCase.ValidarAsync(new ValidarIdentidadExternaDto { ClienteId = cliente.ClienteId });
            Console.WriteLine("Nombre actualizado: " + cliente.RazonSocialONombres);

            // Assert
            Assert.That(cliente.RazonSocialONombres, Is.EqualTo("EMPRESA S.A.C."));
            Assert.That(cliente.DireccionPostal, Is.EqualTo("AV. PERU 123"));
            repoMock.Verify(r => r.UpdateAsync(cliente), Times.Once);
            eventBusMock.Verify(e => e.PublishAsync(It.IsAny<object>()), Times.Once);
        }
    }
}
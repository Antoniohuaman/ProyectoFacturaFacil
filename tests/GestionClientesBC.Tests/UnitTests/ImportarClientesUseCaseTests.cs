using System;
using System.IO;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using GestionClientesBC.Application.UseCases;
using GestionClientesBC.Application.DTOs;
using GestionClientesBC.Application.Interfaces;
using GestionClientesBC.Domain.Entities;
using GestionClientesBC.Domain.ValueObjects;
using GestionClientesBC.Domain.Aggregates;

namespace GestionClientesBC.Tests.UnitTests
{
    [TestFixture]
    public class ImportarClientesUseCaseTests
    {
        [Test]
        public async Task HandleAsync_ImportaClienteCorrectamente()
        {
            // Arrange: lee el archivo Excel real desde Resources
            var ruta = Path.Combine(TestContext.CurrentContext.TestDirectory, "Resources", "clientes.xlsx");
            byte[] archivoExcel = File.ReadAllBytes(ruta);

            var repoMock = new Mock<IGestionClientesRepository>();
            repoMock.Setup(r => r.GetByDocumentoIdentidadAsync(It.IsAny<DocumentoIdentidad>()))
                .ReturnsAsync(null as Cliente); // Simula que no hay cliente existente
            repoMock.Setup(r => r.AddAsync(It.IsAny<Cliente>()))
                .Returns(Task.CompletedTask);

            var useCase = new ImportarClientesUseCase(repoMock.Object);

            var dto = new ImportarClientesDto
            {
                Archivo = archivoExcel,
                NombreArchivo = "clientes.xlsx"
            };

            // Act
            var resultado = await useCase.HandleAsync(dto);

            // Assert
            Assert.That(resultado.Creados, Is.EqualTo(1));
            Assert.That(resultado.Actualizados, Is.EqualTo(0));
            Assert.That(resultado.Errores, Is.Empty);
        }
    }
}
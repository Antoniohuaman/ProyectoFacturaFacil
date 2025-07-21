using System.Threading.Tasks;
using ListaPreciosBC.Application.DTOs;
using ListaPreciosBC.Application.Interfaces;
using ListaPreciosBC.Application.UseCases;
using Moq;
using NUnit.Framework;

namespace ListaPreciosBC.Tests.UnitTests
{
    [TestFixture]
    public class ExportarListasPreciosUseCaseTests
    {
        [Test]
        public async Task HandleAsync_ConFiltros_RetornaArchivoExcel()
        {
            // Arrange
            var filtrosJson = "{\"tipoLista\":\"CANAL\",\"estado\":\"activa\"}";
            var resultadoEsperado = new ResultadoExportacionDto
            {
                NombreArchivo = "listas_precios.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                Archivo = new byte[] { 1, 2, 3 } // Simula contenido binario
            };

            var serviceMock = new Mock<IImportExportService>();
            serviceMock
                .Setup(s => s.ExportarListasPreciosAsync(filtrosJson))
                .ReturnsAsync(resultadoEsperado);

            var useCase = new ExportarListasPreciosUseCase(serviceMock.Object);

            // Act
            var resultado = await useCase.HandleAsync(filtrosJson);

            // Assert
            Assert.That(resultado, Is.Not.Null);
            Assert.That(resultado.NombreArchivo, Is.EqualTo("listas_precios.xlsx"));
            Assert.That(resultado.ContentType, Is.EqualTo("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
            Assert.That(resultado.Archivo, Is.EqualTo(new byte[] { 1, 2, 3 }));
            serviceMock.Verify(s => s.ExportarListasPreciosAsync(filtrosJson), Times.Once);
        }
    }
}
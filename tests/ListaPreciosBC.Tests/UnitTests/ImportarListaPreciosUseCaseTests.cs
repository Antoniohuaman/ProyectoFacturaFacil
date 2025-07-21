using System.IO;
using System.Threading.Tasks;
using ListaPreciosBC.Application.UseCases;
using ListaPreciosBC.Adapters.Output.Persistence.InMemory;
using ListaPreciosBC.Domain.Events;

using Moq;
using NUnit.Framework;


namespace ListaPreciosBC.Tests.UnitTests
{
    [TestFixture]
    public class ImportarListaPreciosUseCaseTests
    {
        [Test]
        public async Task ImportarListaPrecios_DesdeExcel_ProcesaCorrectamente()
        {
            // Arrange
            var repo = new InMemoryListaPrecioRepository();
            var eventBusMock = new Mock<IEventBus>();
            var useCase = new ImportarListaPreciosUseCase(repo, eventBusMock.Object);

            var path = Path.Combine("Resources", "importar_lista_precios.xlsx");
            byte[] archivo = File.ReadAllBytes(path);

            // Act
            var resumen = await useCase.HandleAsync(archivo, "importar_lista_precios.xlsx");

            // Assert
            Assert.That(resumen.Exitos, Is.GreaterThan(0), "Debe haber al menos un éxito");
            Assert.That(resumen.Errores.Count, Is.EqualTo(0), "No debe haber errores de importación");
            eventBusMock.Verify(e => e.PublishAsync(It.IsAny<ListaPrecioCreada>()), Times.AtLeastOnce);
            eventBusMock.Verify(e => e.PublishAsync(It.IsAny<PrecioEspecificoAgregado>()), Times.AtLeastOnce);
        }
    }
}
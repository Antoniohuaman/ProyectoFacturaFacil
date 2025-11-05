using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.UseCases.OperacionesMasivas;
using NUnit.Framework;

namespace GestionInventarioBC.Tests.Application.UseCasesTests.OperacionesMasivas
{
	[TestFixture]
	public class ExportarPlantillaImportacionStockUseCaseTests
	{
		[Test]
		public async Task Ok_GeneraEncabezadosEsperados()
		{
			// Arrange
			var sut = new ExportarPlantillaImportacionStockUseCase();
			var req = new ExportarPlantillaImportacionStockUseCase.Request();
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.CsvContenido, Is.Not.Null.And.Not.Empty);
			Assert.That(res.CsvContenido, Is.EqualTo("SKU;CANTIDAD"));
			Assert.That(res.CsvContenido, Does.Contain(";"));
		}

		[Test]
		public async Task Formato_NoVacio_SeparadoPorPuntoYComa()
		{
			var sut = new ExportarPlantillaImportacionStockUseCase();
			var res = await sut.Handle(new ExportarPlantillaImportacionStockUseCase.Request(), CancellationToken.None);
			Assert.That(res.CsvContenido.Length, Is.GreaterThan(5));
			Assert.That(res.CsvContenido.Split(';').Length, Is.EqualTo(2));
		}
	}
}


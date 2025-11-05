using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.UseCases.OperacionesMasivas;
using NUnit.Framework;

namespace GestionInventarioBC.Tests.Application.UseCasesTests.OperacionesMasivas
{
	[TestFixture]
	public class PrevalidarImportacionStockUseCaseTests
	{
		[Test]
		public async Task Mixto_LineasValidasEInvalidas_DevuelveConteosYErrores()
		{
			// Arrange
			var sut = new PrevalidarImportacionStockUseCase();
			var lineas = new List<PrevalidarImportacionStockUseCase.Linea>
			{
				new("SKU-OK", 5m),            // válida
				new("", 2m),                  // SKU inválido (vacío)
				new("inv@lido", 1m),          // SKU inválido (carácter no permitido)
				new("SKU-NEG", -3m),          // cantidad negativa
			};
			var req = new PrevalidarImportacionStockUseCase.Request(lineas);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.Total, Is.EqualTo(4));
			Assert.That(res.ConErrores, Is.EqualTo(3));
			Assert.That(res.Errores.Count, Is.EqualTo(3));
			Assert.That(res.Errores[0].Index, Is.EqualTo(1));
			Assert.That(res.Errores[1].Index, Is.EqualTo(2));
			Assert.That(res.Errores[2].Index, Is.EqualTo(3));
		}

		[Test]
		public async Task SkuInvalido_TodasErroneas_RegistraErroresPorLinea()
		{
			// Arrange
			var sut = new PrevalidarImportacionStockUseCase();
			var lineas = new List<PrevalidarImportacionStockUseCase.Linea>
			{
				new(" ", 1m),
				new("-NO-EMPIEZA-ALFA", 2m),
				new("MUY-LARGO-123456789012345678901234567890", 3m),
			};
			var req = new PrevalidarImportacionStockUseCase.Request(lineas);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.Total, Is.EqualTo(3));
			Assert.That(res.ConErrores, Is.EqualTo(3));
			Assert.That(res.Errores.Count, Is.EqualTo(3));
			Assert.That(res.Errores[0].Index, Is.EqualTo(0));
		}

		[Test]
		public async Task CantidadNegativa_MarcaError()
		{
			// Arrange
			var sut = new PrevalidarImportacionStockUseCase();
			var lineas = new List<PrevalidarImportacionStockUseCase.Linea>
			{
				new("SKU-1", -1m),
				new("SKU-2", 0m),
				new("SKU-3", 10m)
			};
			var req = new PrevalidarImportacionStockUseCase.Request(lineas);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.Total, Is.EqualTo(3));
			Assert.That(res.ConErrores, Is.EqualTo(1));
			Assert.That(res.Errores.Count, Is.EqualTo(1));
			Assert.That(res.Errores[0].Index, Is.EqualTo(0));
		}

		[Test]
		public async Task TodasValidas_SinErrores()
		{
			// Arrange
			var sut = new PrevalidarImportacionStockUseCase();
			var lineas = new List<PrevalidarImportacionStockUseCase.Linea>
			{
				new("SKU-1", 1m),
				new("SKU-2", 2m),
				new("CAP-258963", 3m)
			};
			var req = new PrevalidarImportacionStockUseCase.Request(lineas);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.Total, Is.EqualTo(3));
			Assert.That(res.ConErrores, Is.EqualTo(0));
			Assert.That(res.Errores.Count, Is.EqualTo(0));
		}
	}
}


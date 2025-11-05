using System;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.UseCases.Politicas;
using NUnit.Framework;
using SharedKernel.Exceptions;

namespace GestionInventarioBC.Tests.Application.UseCasesTests.Politicas
{
	[TestFixture]
	public class ConfigurarRangoStockUseCaseTests
	{
		[Test]
		public async Task HappyPath_Min5_Max50_DevuelveMismosValores()
		{
			// Arrange
			var sut = new ConfigurarRangoStockUseCase();
			var req = new ConfigurarRangoStockUseCase.Request(5m, 50m);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.Minimo, Is.EqualTo(5m));
			Assert.That(res.Maximo, Is.EqualTo(50m));
		}

		[Test]
		public void Regla_MinNoMayorQueMax_Min10Max5_LanzaBusinessRule()
		{
			var sut = new ConfigurarRangoStockUseCase();
			Assert.That(async () => await sut.Handle(new ConfigurarRangoStockUseCase.Request(10m, 5m), CancellationToken.None),
				Throws.TypeOf<BusinessRuleException>());
		}

		[Test]
		public void NoNegativos_MinNegativo_LanzaBusinessRule()
		{
			var sut = new ConfigurarRangoStockUseCase();
			Assert.That(async () => await sut.Handle(new ConfigurarRangoStockUseCase.Request(-1m, 5m), CancellationToken.None),
				Throws.TypeOf<BusinessRuleException>());
		}

		[Test]
		public async Task Bordes_Min0Max0_Aceptado()
		{
			var sut = new ConfigurarRangoStockUseCase();
			var res = await sut.Handle(new ConfigurarRangoStockUseCase.Request(0m, 0m), CancellationToken.None);
			Assert.That(res.Minimo, Is.EqualTo(0m));
			Assert.That(res.Maximo, Is.EqualTo(0m));
		}

		[Test]
		public async Task Bordes_Min0Max1_Aceptado()
		{
			var sut = new ConfigurarRangoStockUseCase();
			var res = await sut.Handle(new ConfigurarRangoStockUseCase.Request(0m, 1m), CancellationToken.None);
			Assert.That(res.Minimo, Is.EqualTo(0m));
			Assert.That(res.Maximo, Is.EqualTo(1m));
		}
	}
}


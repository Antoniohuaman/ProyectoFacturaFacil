using System;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.UseCases.Politicas;
using NUnit.Framework;

namespace GestionInventarioBC.Tests.Application.UseCasesTests.Politicas
{
	[TestFixture]
	public class ConfigurarPoliticaValoracionUseCaseTests
	{
		[Test]
		public async Task Happy_PromedioPonderado_DevuelveMetodo()
		{
			// Arrange
			var sut = new ConfigurarPoliticaValoracionUseCase();
			var req = new ConfigurarPoliticaValoracionUseCase.Request("PromedioPonderado");
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.Metodo, Is.EqualTo("PromedioPonderado"));
		}

		[Test]
		public void MetodoNoSoportado_PEPS_LanzaArgumentException()
		{
			var sut = new ConfigurarPoliticaValoracionUseCase();
			Assert.That(async () => await sut.Handle(new ConfigurarPoliticaValoracionUseCase.Request("PEPS"), CancellationToken.None),
				Throws.TypeOf<ArgumentException>());
		}

		[Test]
		public void MetodoConCasingDiferente_NoCoincide_LanzaArgumentException()
		{
			var sut = new ConfigurarPoliticaValoracionUseCase();
			Assert.That(async () => await sut.Handle(new ConfigurarPoliticaValoracionUseCase.Request("promedioPonderado"), CancellationToken.None),
				Throws.TypeOf<ArgumentException>());
		}

		[Test]
		public void MetodoArbitrario_LanzaArgumentException()
		{
			var sut = new ConfigurarPoliticaValoracionUseCase();
			Assert.That(async () => await sut.Handle(new ConfigurarPoliticaValoracionUseCase.Request("Otro"), CancellationToken.None),
				Throws.TypeOf<ArgumentException>());
		}
	}
}


using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.UseCases.Politicas;
using NUnit.Framework;

namespace GestionInventarioBC.Tests.Application.UseCasesTests.Politicas
{
	[TestFixture]
	public class ConfigurarPoliticaReservaUseCaseTests
	{
		[Test]
		public async Task HappyPath_ModoEstrictamenteDisponibleTrue_EcoEnRespuesta()
		{
			// Arrange
			var sut = new ConfigurarPoliticaReservaUseCase();
			var req = new ConfigurarPoliticaReservaUseCase.Request(true);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.ModoEstrictamenteDisponible, Is.True);
		}

		[Test]
		public async Task HappyPath_ModoEstrictamenteDisponibleFalse_EcoEnRespuesta()
		{
			var sut = new ConfigurarPoliticaReservaUseCase();
			var res = await sut.Handle(new ConfigurarPoliticaReservaUseCase.Request(false), CancellationToken.None);
			Assert.That(res.ModoEstrictamenteDisponible, Is.False);
		}

		[Test]
		public async Task PorDefecto_ConCtorStruct_EsFalse()
		{
			var sut = new ConfigurarPoliticaReservaUseCase();
			var res = await sut.Handle(new ConfigurarPoliticaReservaUseCase.Request(), CancellationToken.None);
			// Nota: para record struct, new Request() invoca el ctor por defecto del struct (no el primario con valor opcional), quedando en false.
			Assert.That(res.ModoEstrictamenteDisponible, Is.False);
		}

		[Test]
		public async Task Idempotencia_LlamadasIndependientes_NoInterfieren()
		{
			var sut = new ConfigurarPoliticaReservaUseCase();
			var r1 = await sut.Handle(new ConfigurarPoliticaReservaUseCase.Request(true), CancellationToken.None);
			var r2 = await sut.Handle(new ConfigurarPoliticaReservaUseCase.Request(false), CancellationToken.None);
			Assert.That(r1.ModoEstrictamenteDisponible, Is.True);
			Assert.That(r2.ModoEstrictamenteDisponible, Is.False);
		}
	}
}


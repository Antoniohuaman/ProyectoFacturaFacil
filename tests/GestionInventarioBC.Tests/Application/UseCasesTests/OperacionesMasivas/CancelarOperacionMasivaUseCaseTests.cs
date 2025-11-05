using System;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.UseCases.OperacionesMasivas;
using NUnit.Framework;

namespace GestionInventarioBC.Tests.Application.UseCasesTests.OperacionesMasivas
{
	[TestFixture]
	public class CancelarOperacionMasivaUseCaseTests
	{
		[Test]
		public async Task Ok_DevuelveCanceladaTrue()
		{
			// Arrange
			var sut = new CancelarOperacionMasivaUseCase();
			var operId = Guid.NewGuid();
			var req = new CancelarOperacionMasivaUseCase.Request(operId);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.OperacionId, Is.EqualTo(operId));
			Assert.That(res.Cancelada, Is.True);
		}

		[Test]
		public async Task IdArbitrario_SiempreCancela()
		{
			var sut = new CancelarOperacionMasivaUseCase();
			var res = await sut.Handle(new CancelarOperacionMasivaUseCase.Request(Guid.NewGuid()), CancellationToken.None);
			Assert.That(res.Cancelada, Is.True);
		}
	}
}


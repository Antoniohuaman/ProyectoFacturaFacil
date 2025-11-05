using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.UseCases.OperacionesMasivas;
using GestionInventarioBC.Domain.ValueObjects;
using GestionInventarioBC.Tests.TestUtils;
using NUnit.Framework;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Tests.Application.UseCasesTests.OperacionesMasivas
{
	[TestFixture]
	public class ResetearStockTotalUseCaseTests
	{
		[Test]
		public async Task Ok_ReseteaTodoElAlmacen_CommitUnaVez()
		{
			// Arrange
			var empresa = EmpresaId.From("EMP-RES-1");
			var est = EstablecimientoId.New();
			var alm = AlmacenId.New();
			var otroAlm = AlmacenId.New();
			var p1 = ProductoId.New();
			var p2 = ProductoId.New();
			var repo = new FakeStockPorAlmacenRepository();
			repo.Ensure(empresa, est, alm, p1, real: 5m, reservado: 2m);
			repo.Ensure(empresa, est, alm, p2, real: 10m, reservado: 4m);
			// Otro almacén no debe afectarse
			repo.Ensure(empresa, est, otroAlm, p1, real: 7m, reservado: 1m);
			var tenant = new FakeTenantContext(empresa);
			var uow = new FakeUnitOfWork();
			var sut = new ResetearStockTotalUseCase(repo, tenant, uow);
			var req = new ResetearStockTotalUseCase.Request(est.Value, alm.Value);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.Afectados, Is.EqualTo(2));
			var s1 = await repo.ObtenerAsync(empresa, est, alm, p1, ct);
			var s2 = await repo.ObtenerAsync(empresa, est, alm, p2, ct);
			Assert.That(s1!.Real.Value, Is.EqualTo(0m));
			Assert.That(s1.Reservado.Value, Is.EqualTo(0m));
			Assert.That(s2!.Real.Value, Is.EqualTo(0m));
			Assert.That(s2.Reservado.Value, Is.EqualTo(0m));
			// Aislamiento
			var sOtro = await repo.ObtenerAsync(empresa, est, otroAlm, p1, ct);
			Assert.That(sOtro!.Real.Value, Is.EqualTo(7m));
			Assert.That(sOtro.Reservado.Value, Is.EqualTo(1m));
			Assert.That(uow.CommitCalls, Is.EqualTo(1));
		}

		[Test]
		public async Task AlmacenVacio_NoAfectaRegistros_IgualCommitUnaVez()
		{
			// Arrange
			var empresa = EmpresaId.From("EMP-RES-2");
			var est = EstablecimientoId.New();
			var alm = AlmacenId.New();
			var repo = new FakeStockPorAlmacenRepository();
			var tenant = new FakeTenantContext(empresa);
			var uow = new FakeUnitOfWork();
			var sut = new ResetearStockTotalUseCase(repo, tenant, uow);
			var req = new ResetearStockTotalUseCase.Request(est.Value, alm.Value);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.Afectados, Is.EqualTo(0));
			Assert.That(uow.CommitCalls, Is.EqualTo(1));
		}
	}
}


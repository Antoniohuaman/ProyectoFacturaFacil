using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.UseCases.OperacionesMasivas;
using GestionInventarioBC.Domain.ValueObjects;
using GestionInventarioBC.Tests.TestUtils;
using NUnit.Framework;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Tests.Application.UseCasesTests.OperacionesMasivas
{
	[TestFixture]
	public class ActualizarStockMasivoUseCaseTests
	{
		[Test]
		public async Task Ok_ActualizaExistencias_CreaSiNoExiste_CommitUnaVez()
		{
			// Arrange
			var empresa = EmpresaId.From("EMP-ACT-1");
			var est = EstablecimientoId.New();
			var alm = AlmacenId.New();
			var p1 = ProductoId.New();
			var p2 = ProductoId.New();
			var tenant = new FakeTenantContext(empresa);
			var uow = new FakeUnitOfWork();
			var catalogo = new FakeCatalogoReadModel();
			catalogo.Seed(empresa.Value, "SKU-1", p1, "Prod 1");
			catalogo.Seed(empresa.Value, "SKU-2", p2, "Prod 2");
			var repo = new FakeStockPorAlmacenRepository();
			repo.Ensure(empresa, est, alm, p1, real: 5m);

			var sut = new ActualizarStockMasivoUseCase(repo, catalogo, tenant, uow);
			var lineas = new List<ActualizarStockMasivoUseCase.Linea>
			{
				new("SKU-1", null, 8m),     // existe → ajusta 5→8
				new("SKU-2", null, 3m),     // no existe → crea en 3
			};
			var req = new ActualizarStockMasivoUseCase.Request(est.Value, alm.Value, lineas);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.Procesados, Is.EqualTo(2));
			var s1 = await repo.ObtenerAsync(empresa, est, alm, p1, ct);
			var s2 = await repo.ObtenerAsync(empresa, est, alm, p2, ct);
			Assert.That(s1!.Real.Value, Is.EqualTo(8m));
			Assert.That(s2!.Real.Value, Is.EqualTo(3m));
			Assert.That(uow.CommitCalls, Is.EqualTo(1));
		}

		[Test]
		public void SkuDesconocido_LanzaNotFound_SinCommit()
		{
			// Arrange
			var empresa = EmpresaId.From("EMP-ACT-2");
			var est = EstablecimientoId.New();
			var alm = AlmacenId.New();
			var tenant = new FakeTenantContext(empresa);
			var uow = new FakeUnitOfWork();
			var catalogo = new FakeCatalogoReadModel();
			var repo = new FakeStockPorAlmacenRepository();
			var sut = new ActualizarStockMasivoUseCase(repo, catalogo, tenant, uow);
			var lineas = new List<ActualizarStockMasivoUseCase.Linea>
			{
				new("SKU-NO", null, 3m)
			};
			var req = new ActualizarStockMasivoUseCase.Request(est.Value, alm.Value, lineas);
			var ct = CancellationToken.None;

			// Act + Assert
			Assert.That(async () => await sut.Handle(req, ct), Throws.TypeOf<NotFoundException>());
			Assert.That(uow.CommitCalls, Is.EqualTo(0));
		}

		[Test]
		public void SkuYProductoIdInconsistentes_LanzaBusinessRule_SinCommit()
		{
			// Arrange
			var empresa = EmpresaId.From("EMP-ACT-3");
			var est = EstablecimientoId.New();
			var alm = AlmacenId.New();
			var tenant = new FakeTenantContext(empresa);
			var uow = new FakeUnitOfWork();
			var catalogo = new FakeCatalogoReadModel();
			var repo = new FakeStockPorAlmacenRepository();
			var pReal = ProductoId.New();
			var pOtro = ProductoId.New();
			catalogo.Seed(empresa.Value, "SKU-1", pReal, "Prod 1");
			var sut = new ActualizarStockMasivoUseCase(repo, catalogo, tenant, uow);
			var lineas = new List<ActualizarStockMasivoUseCase.Linea>
			{
				new("SKU-1", pOtro.Value, 3m)
			};
			var req = new ActualizarStockMasivoUseCase.Request(est.Value, alm.Value, lineas);
			var ct = CancellationToken.None;

			// Act + Assert
			Assert.That(async () => await sut.Handle(req, ct), Throws.TypeOf<BusinessRuleException>());
			Assert.That(uow.CommitCalls, Is.EqualTo(0));
		}

		[Test]
		public void SinSkuNiProductoId_LanzaArgumentException_SinCommit()
		{
			// Arrange
			var empresa = EmpresaId.From("EMP-ACT-4");
			var est = EstablecimientoId.New();
			var alm = AlmacenId.New();
			var tenant = new FakeTenantContext(empresa);
			var uow = new FakeUnitOfWork();
			var catalogo = new FakeCatalogoReadModel();
			var repo = new FakeStockPorAlmacenRepository();
			var sut = new ActualizarStockMasivoUseCase(repo, catalogo, tenant, uow);
			var lineas = new List<ActualizarStockMasivoUseCase.Linea>
			{
				new(null, null, 3m)
			};
			var req = new ActualizarStockMasivoUseCase.Request(est.Value, alm.Value, lineas);
			var ct = CancellationToken.None;

			// Act + Assert
			Assert.That(async () => await sut.Handle(req, ct), Throws.TypeOf<ArgumentException>());
			Assert.That(uow.CommitCalls, Is.EqualTo(0));
		}
	}
}


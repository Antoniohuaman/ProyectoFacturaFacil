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
	public class ExportarListadoStockUseCaseTests
	{
		[Test]
		public async Task Ok_ListaSoloDelAlmacen_EnriquecidoConCatalogo()
		{
			// Arrange
			var empresa = EmpresaId.From("EMP-EXP-1");
			var est = EstablecimientoId.New();
			var alm = AlmacenId.New();
			var otroAlm = AlmacenId.New();
			var p1 = ProductoId.New();
			var p2 = ProductoId.New();
			var catalogo = new FakeCatalogoReadModel();
			catalogo.Seed(empresa.Value, "SKU-1", p1, "Prod 1");
			catalogo.Seed(empresa.Value, "SKU-2", p2, "Prod 2");
			var repo = new FakeStockPorAlmacenRepository();
			repo.Ensure(empresa, est, alm, p1, real: 5m);
			repo.Ensure(empresa, est, alm, p2, real: 10m, reservado: 2m);
			// Otros no deben aparecer
			repo.Ensure(empresa, est, otroAlm, p1, real: 99m);
			var tenant = new FakeTenantContext(empresa);
			var sut = new ExportarListadoStockUseCase(repo, tenant, catalogo);
			var req = new ExportarListadoStockUseCase.Request(est.Value, alm.Value);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.Items.Count, Is.EqualTo(2));
			// Enriquecimiento: Sku y Nombre
			Assert.That(res.Items, Has.Some.Matches<ExportarListadoStockUseCase.Item>(i => i.Sku == "SKU-1" && i.Nombre == "Prod 1" && i.Real == 5m));
			Assert.That(res.Items, Has.Some.Matches<ExportarListadoStockUseCase.Item>(i => i.Sku == "SKU-2" && i.Nombre == "Prod 2" && i.Real == 10m && i.Reservado == 2m && i.Disponible == 8m));
		}

		[Test]
		public async Task Aislamiento_EmpresaYAlmacen_NoIncluyeOtros()
		{
			var empresaA = EmpresaId.From("EMP-EXP-2A");
			var empresaB = EmpresaId.From("EMP-EXP-2B");
			var est = EstablecimientoId.New();
			var alm = AlmacenId.New();
			var p = ProductoId.New();
			var catalogo = new FakeCatalogoReadModel();
			catalogo.Seed(empresaA.Value, "SKU-A", p, "Prod A");
			catalogo.Seed(empresaB.Value, "SKU-B", p, "Prod B");
			var repo = new FakeStockPorAlmacenRepository();
			// Empresa A (target)
			repo.Ensure(empresaA, est, alm, p, real: 1m);
			// Empresa B (debe quedar fuera)
			repo.Ensure(empresaB, est, alm, p, real: 2m);

			var sut = new ExportarListadoStockUseCase(repo, new FakeTenantContext(empresaA), catalogo);
			var res = await sut.Handle(new ExportarListadoStockUseCase.Request(est.Value, alm.Value), CancellationToken.None);
			Assert.That(res.Items.Count, Is.EqualTo(1));
			Assert.That(res.Items[0].Sku, Is.EqualTo("SKU-A"));
		}
	}
}


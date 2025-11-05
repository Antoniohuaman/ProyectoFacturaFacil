using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.UseCases.Consultas;
using GestionInventarioBC.Tests.TestUtils;
using NUnit.Framework;
using SharedKernel.ValueObjects;
using GestionInventarioBC.Domain.ValueObjects;

namespace GestionInventarioBC.Tests.Application.UseCasesTests.Consultas
{
	[TestFixture]
	public class ListarStockPorAlmacenUseCaseTests
	{
		[Test]
		public async Task Handle_Paginacion_DevuelveTotalEItemsEsperados()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-LS-1");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();

			var tenant = new FakeTenantContext(empresaId);
			var catalogo = new FakeCatalogoReadModel();
			var stockRepo = new FakeStockPorAlmacenRepository();

			// Seed 25 productos en el mismo almacén
			for (int i = 1; i <= 25; i++)
			{
				var pid = ProductoId.New();
				var sku = $"sku-{i:D2}";
				catalogo.Seed(empresaId.Value, sku, pid, $"Nombre {i:D2}");
				stockRepo.Ensure(empresaId, estId, almId, pid, real: 10 + i, reservado: i % 3);
			}

			var sut = new ListarStockPorAlmacenUseCase(stockRepo, tenant, catalogo);
			var req = new ListarStockPorAlmacenUseCase.Request(estId.Value, almId.Value, Page: 2, PageSize: 10);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.Total, Is.EqualTo(25));
			Assert.That(res.Items.Count, Is.EqualTo(10));
			// No asumimos orden específico del repositorio; validamos que son items válidos con Sku/Nombre poblados
			Assert.That(res.Items.All(it => !string.IsNullOrWhiteSpace(it.Sku) && !string.IsNullOrWhiteSpace(it.Nombre)), Is.True);
		}

		[Test]
		public async Task Handle_EnriquecimientoSkuYNombre_CompletadosDesdeCatalogo()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-LS-2");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();
			var pid = ProductoId.New();

			var tenant = new FakeTenantContext(empresaId);
			var catalogo = new FakeCatalogoReadModel();
			var stockRepo = new FakeStockPorAlmacenRepository();

			catalogo.Seed(empresaId.Value, "sku-x", pid, "Nombre X");
			stockRepo.Ensure(empresaId, estId, almId, pid, real: 5m, reservado: 2m);

			var sut = new ListarStockPorAlmacenUseCase(stockRepo, tenant, catalogo);
			var req = new ListarStockPorAlmacenUseCase.Request(estId.Value, almId.Value, Page: 1, PageSize: 10);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.Total, Is.EqualTo(1));
			Assert.That(res.Items[0].Sku, Is.EqualTo("sku-x"));
			Assert.That(res.Items[0].Nombre, Is.EqualTo("Nombre X"));
			Assert.That(res.Items[0].Real, Is.EqualTo(5m));
			Assert.That(res.Items[0].Reservado, Is.EqualTo(2m));
			Assert.That(res.Items[0].Disponible, Is.EqualTo(3m));
		}

		[Test]
		public async Task Handle_AlmacenEquivocado_NoIncluyeItemsDeOtroAlmacen()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-LS-3");
			var estId = EstablecimientoId.New();
			var almOk = AlmacenId.New();
			var almOtro = AlmacenId.New();
			var pid = ProductoId.New();

			var tenant = new FakeTenantContext(empresaId);
			var catalogo = new FakeCatalogoReadModel();
			var stockRepo = new FakeStockPorAlmacenRepository();

			catalogo.Seed(empresaId.Value, "sku-1", pid, "Item 1");
			stockRepo.Ensure(empresaId, estId, almOk, pid, real: 8m, reservado: 1m);
			stockRepo.Ensure(empresaId, estId, almOtro, pid, real: 99m, reservado: 50m);

			var sut = new ListarStockPorAlmacenUseCase(stockRepo, tenant, catalogo);
			var req = new ListarStockPorAlmacenUseCase.Request(estId.Value, almOk.Value, Page: 1, PageSize: 10);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert: solo ve el del almacén solicitado
			Assert.That(res.Total, Is.EqualTo(1));
			Assert.That(res.Items[0].Real, Is.EqualTo(8m));
			Assert.That(res.Items[0].Reservado, Is.EqualTo(1m));
		}

		[Test]
		public async Task Handle_AislamientoPorEmpresa_NoCruzaStocks()
		{
			// Arrange
			var empresaA = new EmpresaId("EMP-A");
			var empresaB = new EmpresaId("EMP-B");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();
			var pid = ProductoId.New();

			var tenantA = new FakeTenantContext(empresaA);
			var catalogo = new FakeCatalogoReadModel();
			var stockRepo = new FakeStockPorAlmacenRepository();

			catalogo.Seed(empresaA.Value, "sku", pid, "Nombre");
			stockRepo.Ensure(empresaA, estId, almId, pid, real: 1m, reservado: 0m);
			// Empresa B: mismo pid y sku no deben influir
			catalogo.Seed(empresaB.Value, "sku", pid, "Nombre B");
			stockRepo.Ensure(empresaB, estId, almId, pid, real: 1000m, reservado: 0m);

			var sut = new ListarStockPorAlmacenUseCase(stockRepo, tenantA, catalogo);
			var req = new ListarStockPorAlmacenUseCase.Request(estId.Value, almId.Value, Page: 1, PageSize: 10);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.Total, Is.EqualTo(1));
			Assert.That(res.Items[0].Real, Is.EqualTo(1m));
		}
	}
}


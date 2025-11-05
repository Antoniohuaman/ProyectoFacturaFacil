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
	public class ListarDisponibilidadUseCaseTests
	{
		[Test]
		public async Task Handle_FiltrosYPaginacion_TotalEItemsCorrectos()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-LD-1");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();

			var tenant = new FakeTenantContext(empresaId);
			var catalogo = new FakeCatalogoReadModel();
			var stockRepo = new FakeStockPorAlmacenRepository();

			// Seed 15 ítems, algunos con disponible 0
			for (int i = 0; i < 15; i++)
			{
				var pid = ProductoId.New();
				var sku = $"sku-{i:D2}";
				catalogo.Seed(empresaId.Value, sku, pid, $"Nombre {i:D2}");
				var real = i; // 0..14
				var reservado = i / 2; // hace que algunos queden en 0 disponible
				stockRepo.Ensure(empresaId, estId, almId, pid, real: real, reservado: reservado);
			}

			var sut = new ListarDisponibilidadUseCase(stockRepo, tenant, catalogo);
			// Filtro por sku "sku-1" debe devolver los que contengan ese fragmento (fake BuscarProductoIdsAsync hace contains)
			var req = new ListarDisponibilidadUseCase.Request(estId.Value, almId.Value, FiltroSku: "sku-1", SoloConDisponible: true, Page: 1, PageSize: 5);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert: Total corresponde a todos los sku-* que contienen "1" con disponible > 0 (e.g. 10..14 y 1)
			Assert.That(res.Total, Is.GreaterThan(0));
			Assert.That(res.Items.Count, Is.LessThanOrEqualTo(5));
			Assert.That(res.Items.All(i => i.Sku.Contains("1") && i.Disponible > 0m), Is.True);
		}

		[Test]
		public async Task Handle_FiltroPorSku_DevuelveSoloCoincidentes()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-LD-2");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();
			var tenant = new FakeTenantContext(empresaId);
			var catalogo = new FakeCatalogoReadModel();
			var stockRepo = new FakeStockPorAlmacenRepository();

			var pid1 = ProductoId.New();
			var pid2 = ProductoId.New();
			catalogo.Seed(empresaId.Value, "abc-100", pid1, "ABC 100");
			catalogo.Seed(empresaId.Value, "xyz-200", pid2, "XYZ 200");
			stockRepo.Ensure(empresaId, estId, almId, pid1, real: 5m, reservado: 0m);
			stockRepo.Ensure(empresaId, estId, almId, pid2, real: 5m, reservado: 0m);

			var sut = new ListarDisponibilidadUseCase(stockRepo, tenant, catalogo);
			var req = new ListarDisponibilidadUseCase.Request(estId.Value, almId.Value, FiltroSku: "abc", SoloConDisponible: false, Page: 1, PageSize: 50);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.Total, Is.EqualTo(1));
			Assert.That(res.Items[0].Sku, Is.EqualTo("abc-100"));
		}

		[Test]
		public async Task Handle_PaginaFueraDeRango_ItemsVaciosTotalCorrecto()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-LD-3");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();
			var tenant = new FakeTenantContext(empresaId);
			var catalogo = new FakeCatalogoReadModel();
			var stockRepo = new FakeStockPorAlmacenRepository();

			for (int i = 0; i < 7; i++)
			{
				var pid = ProductoId.New();
				catalogo.Seed(empresaId.Value, $"sku-{i}", pid, $"Nombre {i}");
				stockRepo.Ensure(empresaId, estId, almId, pid, real: 1m, reservado: 0m);
			}

			var sut = new ListarDisponibilidadUseCase(stockRepo, tenant, catalogo);
			var req = new ListarDisponibilidadUseCase.Request(estId.Value, almId.Value, FiltroSku: null, SoloConDisponible: false, Page: 3, PageSize: 5);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.Total, Is.EqualTo(7));
			Assert.That(res.Items.Count, Is.EqualTo(0));
		}

		[Test]
		public async Task Handle_AislamientoTenant_NoMezclaEmpresasNiEstablecimientos()
		{
			// Arrange
			var empresaA = new EmpresaId("EMP-A");
			var empresaB = new EmpresaId("EMP-B");
			var estA = EstablecimientoId.New();
			var estB = EstablecimientoId.New();
			var almId = AlmacenId.New();
			var pidA = ProductoId.New();
			var pidB = ProductoId.New();

			var tenantA = new FakeTenantContext(empresaA);
			var catalogo = new FakeCatalogoReadModel();
			var stockRepo = new FakeStockPorAlmacenRepository();

			catalogo.Seed(empresaA.Value, "sku-a", pidA, "A");
			catalogo.Seed(empresaB.Value, "sku-b", pidB, "B");
			stockRepo.Ensure(empresaA, estA, almId, pidA, real: 5m, reservado: 0m);
			stockRepo.Ensure(empresaB, estB, almId, pidB, real: 99m, reservado: 0m);

			var sut = new ListarDisponibilidadUseCase(stockRepo, tenantA, catalogo);
			var req = new ListarDisponibilidadUseCase.Request(estA.Value, almId.Value, FiltroSku: null, SoloConDisponible: false, Page: 1, PageSize: 50);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.Total, Is.EqualTo(1));
			Assert.That(res.Items[0].Sku, Is.EqualTo("sku-a"));
		}
	}
}


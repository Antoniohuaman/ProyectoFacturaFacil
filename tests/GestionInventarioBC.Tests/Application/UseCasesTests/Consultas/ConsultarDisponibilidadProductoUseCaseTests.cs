using System;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.UseCases.Consultas;
using GestionInventarioBC.Tests.TestUtils;
using NUnit.Framework;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;
using GestionInventarioBC.Domain.ValueObjects;

namespace GestionInventarioBC.Tests.Application.UseCasesTests.Consultas
{
	[TestFixture]
	public class ConsultarDisponibilidadProductoUseCaseTests
	{
		[Test]
		public async Task Handle_SkuValido_RetornaRealReservadoYDisponible()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-CD-1");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();
			var pid = ProductoId.New();
			var sku = "sku-123";

			var tenant = new FakeTenantContext(empresaId);
			var catalogo = new FakeCatalogoReadModel();
			var stockRepo = new FakeStockPorAlmacenRepository();

			catalogo.Seed(empresaId.Value, sku, pid, "Prod 123");
			var stock = stockRepo.Ensure(empresaId, estId, almId, pid, real: 15m, reservado: 4m);

			var sut = new ConsultarDisponibilidadProductoUseCase(stockRepo, tenant, catalogo);
			var req = new ConsultarDisponibilidadProductoUseCase.Request(estId.Value, almId.Value, sku);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.Sku, Is.EqualTo(sku));
			Assert.That(res.Nombre, Is.EqualTo("Prod 123"));
			Assert.That(res.Real, Is.EqualTo(15m));
			Assert.That(res.Reservado, Is.EqualTo(4m));
			Assert.That(res.Disponible, Is.EqualTo(11m));
		}

		[Test]
		public void Handle_SkuDesconocido_LanzaNotFound()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-CD-2");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();
			var tenant = new FakeTenantContext(empresaId);
			var catalogo = new FakeCatalogoReadModel();
			var stockRepo = new FakeStockPorAlmacenRepository();

			var sut = new ConsultarDisponibilidadProductoUseCase(stockRepo, tenant, catalogo);
			var req = new ConsultarDisponibilidadProductoUseCase.Request(estId.Value, almId.Value, "no-existe");
			var ct = CancellationToken.None;

			// Act + Assert
			Assert.That(() => sut.Handle(req, ct), Throws.TypeOf<NotFoundException>());
		}

		[Test]
		public void Handle_NoExisteStockEnAlmacen_LanzaNotFound()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-CD-3");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();
			var pid = ProductoId.New();
			var sku = "sku-x";

			var tenant = new FakeTenantContext(empresaId);
			var catalogo = new FakeCatalogoReadModel();
			var stockRepo = new FakeStockPorAlmacenRepository();

			catalogo.Seed(empresaId.Value, sku, pid, "X");
			// No se seed-ea stock en ese almacén

			var sut = new ConsultarDisponibilidadProductoUseCase(stockRepo, tenant, catalogo);
			var req = new ConsultarDisponibilidadProductoUseCase.Request(estId.Value, almId.Value, sku);
			var ct = CancellationToken.None;

			// Act + Assert
			Assert.That(() => sut.Handle(req, ct), Throws.TypeOf<NotFoundException>());
		}

	[Test]
	public async Task Handle_AislamientoPorAlmacenYOtraEmpresa_NoCruzaStocks()
		{
			// Arrange
			var empresaA = new EmpresaId("EMP-A");
			var empresaB = new EmpresaId("EMP-B");
			var estA = EstablecimientoId.New();
			var almA = AlmacenId.New();
			var almOtro = AlmacenId.New();
			var pid = ProductoId.New();
			var sku = "sku-iso";

			var tenantA = new FakeTenantContext(empresaA);
			var catalogo = new FakeCatalogoReadModel();
			var stockRepo = new FakeStockPorAlmacenRepository();

			// Seed empresa A
			catalogo.Seed(empresaA.Value, sku, pid, "Prod ISO");
			stockRepo.Ensure(empresaA, estA, almA, pid, real: 3m, reservado: 1m);
			// Otro almacén de A, no debería afectar
			stockRepo.Ensure(empresaA, estA, almOtro, pid, real: 100m, reservado: 50m);
			// Empresa B mapea el mismo sku a otro producto (aislamiento por empresa)
			catalogo.Seed(empresaB.Value, sku, ProductoId.New(), "Otro Prod");

			var sut = new ConsultarDisponibilidadProductoUseCase(stockRepo, tenantA, catalogo);
			var req = new ConsultarDisponibilidadProductoUseCase.Request(estA.Value, almA.Value, sku);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert: usa sólo el stock de A/almA
			Assert.That(res.Real, Is.EqualTo(3m));
			Assert.That(res.Reservado, Is.EqualTo(1m));
			Assert.That(res.Disponible, Is.EqualTo(2m));
		}

		[Test]
		public void Handle_SkuMismapeaAProductoSinStock_ResultaNotFound()
		{
			// Nota: Este caso corresponde al escenario de “Sku + ProductoId inconsistentes” solicitado.
			// En este UseCase la API solo recibe SKU; si el SKU mapea a un ProductoId distinto al que tiene stock,
			// el resultado natural es NotFound (no BusinessRuleException) porque no hay manera de pasar ProductoId aquí.

			// Arrange
			var empresaId = new EmpresaId("EMP-CD-5");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();
			var pidConStock = ProductoId.New();
			var pidDeCatalogo = ProductoId.New(); // distinto
			var sku = "sku-mismatch";

			var tenant = new FakeTenantContext(empresaId);
			var catalogo = new FakeCatalogoReadModel();
			var stockRepo = new FakeStockPorAlmacenRepository();

			// stock para pidConStock
			stockRepo.Ensure(empresaId, estId, almId, pidConStock, real: 10m, reservado: 0m);
			// catálogo devuelve pidDeCatalogo para el sku dado
			catalogo.Seed(empresaId.Value, sku, pidDeCatalogo, "Nombre");

			var sut = new ConsultarDisponibilidadProductoUseCase(stockRepo, tenant, catalogo);
			var req = new ConsultarDisponibilidadProductoUseCase.Request(estId.Value, almId.Value, sku);
			var ct = CancellationToken.None;

			// Act + Assert
			Assert.That(() => sut.Handle(req, ct), Throws.TypeOf<NotFoundException>());
		}
	}
}


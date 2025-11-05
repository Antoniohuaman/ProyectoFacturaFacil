using System;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.UseCases.Reservas;
using GestionInventarioBC.Domain.Aggregates;
using GestionInventarioBC.Domain.ValueObjects;
using GestionInventarioBC.Tests.TestUtils;
using NUnit.Framework;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Tests.Application.UseCasesTests.Reservas
{
	[TestFixture]
	public class CrearReservaStockUseCaseTests
	{
		[Test]
		public async Task Handle_ProductoIdDirecto_CreaReservaYDescuentaDisponible_CommitUnaVez()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-1");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();
			var prodId = ProductoId.New();
			var otroAlmId = AlmacenId.New();

			var tenant = new FakeTenantContext(empresaId);
			var uow = new FakeUnitOfWork();
			var catalogo = new FakeCatalogoReadModel();
			var stockRepo = new FakeStockPorAlmacenRepository();
			var reservaRepo = new FakeReservaStockRepository();

			// stock inicial: real 10, reservado 3 => disponible 7
			var stock = stockRepo.Ensure(empresaId, estId, almId, prodId, real: 10m, reservado: 3m);
			// otro almacén para validar aislamiento
			var stockOtro = stockRepo.Ensure(empresaId, estId, otroAlmId, prodId, real: 5m, reservado: 0m);

			var sut = new CrearReservaStockUseCase(stockRepo, reservaRepo, catalogo, tenant, uow);
			var req = new CrearReservaStockUseCase.Request(
				EstablecimientoId: estId.Value,
				AlmacenId: almId.Value,
				Sku: null,
				ProductoId: prodId.Value,
				Cantidad: 5m,
				VenceEn: DateTimeOffset.UtcNow.AddDays(2)
			);

			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.ReservaId, Is.Not.EqualTo(Guid.Empty));
			var stockPost = await stockRepo.ObtenerAsync(empresaId, estId, almId, prodId, ct);
			Assert.That(stockPost, Is.Not.Null);
			// Disponible = Real - Reservado, al reservar aumenta Reservado
			Assert.That(stockPost!.Real.Value, Is.EqualTo(10m));
			Assert.That(stockPost.Reservado.Value, Is.EqualTo(8m));
			// Aislamiento: otro almacén sin cambios
			var stockOtroPost = await stockRepo.ObtenerAsync(empresaId, estId, otroAlmId, prodId, ct);
			Assert.That(stockOtroPost!.Real.Value, Is.EqualTo(stockOtro.Real.Value));
			Assert.That(stockOtroPost!.Reservado.Value, Is.EqualTo(stockOtro.Reservado.Value));

			Assert.That(uow.CommitCalls, Is.EqualTo(1));
		}

		[Test]
		public void Handle_SkuDesconocido_LanzaNotFound_SinCommit()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-2");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();

			var tenant = new FakeTenantContext(empresaId);
			var uow = new FakeUnitOfWork();
			var catalogo = new FakeCatalogoReadModel(); // sin seed para este SKU
			var stockRepo = new FakeStockPorAlmacenRepository();
			var reservaRepo = new FakeReservaStockRepository();

			var sut = new CrearReservaStockUseCase(stockRepo, reservaRepo, catalogo, tenant, uow);
			var req = new CrearReservaStockUseCase.Request(
				EstablecimientoId: estId.Value,
				AlmacenId: almId.Value,
				Sku: "ABC-123",
				ProductoId: null,
				Cantidad: 1m,
				VenceEn: null
			);

			var ct = CancellationToken.None;

			// Act + Assert
			Assert.That(() => sut.Handle(req, ct), Throws.TypeOf<NotFoundException>());
			Assert.That(uow.CommitCalls, Is.EqualTo(0));
		}

		[Test]
		public void Handle_DisponibleInsuficiente_LanzaBusinessRule_SinCommit()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-3");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();
			var prodId = ProductoId.New();

			var tenant = new FakeTenantContext(empresaId);
			var uow = new FakeUnitOfWork();
			var catalogo = new FakeCatalogoReadModel();
			var stockRepo = new FakeStockPorAlmacenRepository();
			var reservaRepo = new FakeReservaStockRepository();

			// real 10, reservado 9 => disponible 1; pedimos 2
			stockRepo.Ensure(empresaId, estId, almId, prodId, real: 10m, reservado: 9m);

			var sut = new CrearReservaStockUseCase(stockRepo, reservaRepo, catalogo, tenant, uow);
			var req = new CrearReservaStockUseCase.Request(
				EstablecimientoId: estId.Value,
				AlmacenId: almId.Value,
				Sku: null,
				ProductoId: prodId.Value,
				Cantidad: 2m,
				VenceEn: null
			);

			var ct = CancellationToken.None;

			// Act + Assert
			Assert.That(() => sut.Handle(req, ct), Throws.TypeOf<BusinessRuleException>());
			Assert.That(uow.CommitCalls, Is.EqualTo(0));
		}

		[Test]
		public void Handle_SkuYProductoIdNoCorresponden_LanzaBusinessRule()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-4");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();
			var prodIdCatalogo = ProductoId.New();
			var prodIdSolicitud = ProductoId.New(); // distinto

			var tenant = new FakeTenantContext(empresaId);
			var uow = new FakeUnitOfWork();
			var catalogo = new FakeCatalogoReadModel();
			var stockRepo = new FakeStockPorAlmacenRepository();
			var reservaRepo = new FakeReservaStockRepository();

			// seed del SKU apuntando a prodIdCatalogo
			catalogo.Seed(empresaId.Value, "SKU-1", prodIdCatalogo, "Producto 1");

			var sut = new CrearReservaStockUseCase(stockRepo, reservaRepo, catalogo, tenant, uow);
			var req = new CrearReservaStockUseCase.Request(
				EstablecimientoId: estId.Value,
				AlmacenId: almId.Value,
				Sku: "SKU-1",
				ProductoId: prodIdSolicitud.Value, // no corresponde con el del catálogo
				Cantidad: 1m,
				VenceEn: null
			);

			var ct = CancellationToken.None;

			// Act + Assert
			Assert.That(() => sut.Handle(req, ct), Throws.TypeOf<BusinessRuleException>());
			Assert.That(uow.CommitCalls, Is.EqualTo(0));
		}
	}
}


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
	public class LiberarReservaStockUseCaseTests
	{
		[Test]
		public async Task Handle_LiberaReserva_PersisteYActualizaStock_CommitUnaVez()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-L1");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();
			var prodId = ProductoId.New();
			var otroAlmId = AlmacenId.New();

			var tenant = new FakeTenantContext(empresaId);
			var uow = new FakeUnitOfWork();
			var stockRepo = new FakeStockPorAlmacenRepository();
			var reservaRepo = new FakeReservaStockRepository();

			// Stock con real 10 y reservado 5
			stockRepo.Ensure(empresaId, estId, almId, prodId, real: 10m, reservado: 5m);
			stockRepo.Ensure(empresaId, estId, otroAlmId, prodId, real: 7m, reservado: 1m); // aislamiento

			var reserva = ReservaStock.Crear(empresaId, estId, almId, prodId, CantidadStock.From(5m), DateTimeOffset.UtcNow.AddDays(1));
			reservaRepo.Add(reserva);

			var sut = new LiberarReservaStockUseCase(reservaRepo, stockRepo, tenant, uow);
			var req = new LiberarReservaStockUseCase.Request(estId.Value, almId.Value, reserva.ReservaId);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.Ok, Is.True);
			var stockPost = await stockRepo.ObtenerAsync(empresaId, estId, almId, prodId, ct);
			Assert.That(stockPost!.Real.Value, Is.EqualTo(10m));
			Assert.That(stockPost!.Reservado.Value, Is.EqualTo(0m));
			// Aislamiento: otro almacén sin cambios
			var stockOtroPost = await stockRepo.ObtenerAsync(empresaId, estId, otroAlmId, prodId, ct);
			Assert.That(stockOtroPost!.Reservado.Value, Is.EqualTo(1m));
			Assert.That(uow.CommitCalls, Is.EqualTo(1));
		}

		[Test]
		public async Task Handle_SegundaLiberacion_NoCambiaEstado_NiStock_NoNuevoCommit()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-L2");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();
			var prodId = ProductoId.New();
			var tenant = new FakeTenantContext(empresaId);
			var uow = new FakeUnitOfWork();
			var stockRepo = new FakeStockPorAlmacenRepository();
			var reservaRepo = new FakeReservaStockRepository();

			stockRepo.Ensure(empresaId, estId, almId, prodId, real: 4m, reservado: 2m);
			var reserva = ReservaStock.Crear(empresaId, estId, almId, prodId, CantidadStock.From(2m), DateTimeOffset.UtcNow.AddDays(1));
			reservaRepo.Add(reserva);

			var sut = new LiberarReservaStockUseCase(reservaRepo, stockRepo, tenant, uow);
			var req = new LiberarReservaStockUseCase.Request(estId.Value, almId.Value, reserva.ReservaId);
			var ct = CancellationToken.None;

			// Act
			var res1 = await sut.Handle(req, ct);

			// Assert (primera vez)
			Assert.That(res1.Ok, Is.True);
			Assert.That(uow.CommitCalls, Is.EqualTo(1));
			var stockPost1 = await stockRepo.ObtenerAsync(empresaId, estId, almId, prodId, ct);
			Assert.That(stockPost1!.Reservado.Value, Is.EqualTo(0m));

			// Act 2: Segunda liberación
			// La implementación de dominio lanza BusinessRuleException si no está Pendiente;
			// verificamos que no hay efectos colaterales ni nuevo commit.
			Assert.That(() => sut.Handle(req, ct), Throws.TypeOf<BusinessRuleException>());
			Assert.That(uow.CommitCalls, Is.EqualTo(1));
			var stockPost2 = await stockRepo.ObtenerAsync(empresaId, estId, almId, prodId, ct);
			Assert.That(stockPost2!.Reservado.Value, Is.EqualTo(0m));
		}

		[Test]
		public void Handle_ReservaInexistente_LanzaNotFound()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-L3");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();
			var tenant = new FakeTenantContext(empresaId);
			var uow = new FakeUnitOfWork();
			var stockRepo = new FakeStockPorAlmacenRepository();
			var reservaRepo = new FakeReservaStockRepository();

			var sut = new LiberarReservaStockUseCase(reservaRepo, stockRepo, tenant, uow);
			var req = new LiberarReservaStockUseCase.Request(estId.Value, almId.Value, Guid.NewGuid());
			var ct = CancellationToken.None;

			// Act + Assert
			Assert.That(() => sut.Handle(req, ct), Throws.TypeOf<NotFoundException>());
		}

		[Test]
		public void Handle_ReservaVencida_ReglaDominioImpideLiberar()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-L4");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();
			var prodId = ProductoId.New();
			var tenant = new FakeTenantContext(empresaId);
			var uow = new FakeUnitOfWork();
			var stockRepo = new FakeStockPorAlmacenRepository();
			var reservaRepo = new FakeReservaStockRepository();

			stockRepo.Ensure(empresaId, estId, almId, prodId, real: 3m, reservado: 1m);
			var r = ReservaStock.Crear(empresaId, estId, almId, prodId, CantidadStock.From(1m), DateTimeOffset.UtcNow.AddSeconds(-1));
			r.Vencer();
			reservaRepo.Add(r);

			var sut = new LiberarReservaStockUseCase(reservaRepo, stockRepo, tenant, uow);
			var req = new LiberarReservaStockUseCase.Request(estId.Value, almId.Value, r.ReservaId);
			var ct = CancellationToken.None;

			// Act + Assert
			Assert.That(() => sut.Handle(req, ct), Throws.TypeOf<BusinessRuleException>());
			Assert.That(uow.CommitCalls, Is.EqualTo(0));
		}
	}
}


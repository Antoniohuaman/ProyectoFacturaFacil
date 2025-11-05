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
	public class ConsumirReservaStockUseCaseTests
	{
		[Test]
		public async Task Handle_ConsumeReserva_Total_ReduceReservadoYEgregaReal_CommitUnaVez()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-C1");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();
			var prodId = ProductoId.New();
			var otroAlmId = AlmacenId.New();

			var tenant = new FakeTenantContext(empresaId);
			var uow = new FakeUnitOfWork();
			var stockRepo = new FakeStockPorAlmacenRepository();
			var reservaRepo = new FakeReservaStockRepository();

			// Stock: real 10, reservado 4; reserva por 4
			stockRepo.Ensure(empresaId, estId, almId, prodId, real: 10m, reservado: 4m);
			stockRepo.Ensure(empresaId, estId, otroAlmId, prodId, real: 5m, reservado: 0m); // aislamiento
			var reserva = ReservaStock.Crear(empresaId, estId, almId, prodId, CantidadStock.From(4m), DateTimeOffset.UtcNow.AddDays(1));
			reservaRepo.Add(reserva);

			var sut = new ConsumirReservaStockUseCase(reservaRepo, stockRepo, tenant, uow);
			var req = new ConsumirReservaStockUseCase.Request(estId.Value, almId.Value, reserva.ReservaId);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.Ok, Is.True);
			var stockPost = await stockRepo.ObtenerAsync(empresaId, estId, almId, prodId, ct);
			Assert.That(stockPost!.Reservado.Value, Is.EqualTo(0m));
			Assert.That(stockPost!.Real.Value, Is.EqualTo(6m));
			var stockOtroPost = await stockRepo.ObtenerAsync(empresaId, estId, otroAlmId, prodId, ct);
			Assert.That(stockOtroPost!.Real.Value, Is.EqualTo(5m));
			Assert.That(uow.CommitCalls, Is.EqualTo(1));
		}

		[Test]
		public void Handle_MontoConsumirMayorQueReservado_LanzaBusinessRule_SinCommit()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-C2");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();
			var prodId = ProductoId.New();
			var tenant = new FakeTenantContext(empresaId);
			var uow = new FakeUnitOfWork();
			var stockRepo = new FakeStockPorAlmacenRepository();
			var reservaRepo = new FakeReservaStockRepository();

			// Reserva por 3, pero stock tiene reservado 1 => al liberar reserva por 3 se violan reglas
			stockRepo.Ensure(empresaId, estId, almId, prodId, real: 5m, reservado: 1m);
			var reserva = ReservaStock.Crear(empresaId, estId, almId, prodId, CantidadStock.From(3m), DateTimeOffset.UtcNow.AddDays(1));
			reservaRepo.Add(reserva);

			var sut = new ConsumirReservaStockUseCase(reservaRepo, stockRepo, tenant, uow);
			var req = new ConsumirReservaStockUseCase.Request(estId.Value, almId.Value, reserva.ReservaId);
			var ct = CancellationToken.None;

			// Act + Assert
			Assert.That(() => sut.Handle(req, ct), Throws.TypeOf<BusinessRuleException>());
			Assert.That(uow.CommitCalls, Is.EqualTo(0));
		}

		[Test]
		public void Handle_ReservaInexistente_LanzaNotFound()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-C3");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();
			var tenant = new FakeTenantContext(empresaId);
			var uow = new FakeUnitOfWork();
			var stockRepo = new FakeStockPorAlmacenRepository();
			var reservaRepo = new FakeReservaStockRepository();

			var sut = new ConsumirReservaStockUseCase(reservaRepo, stockRepo, tenant, uow);
			var req = new ConsumirReservaStockUseCase.Request(estId.Value, almId.Value, Guid.NewGuid());
			var ct = CancellationToken.None;

			// Act + Assert
			Assert.That(() => sut.Handle(req, ct), Throws.TypeOf<NotFoundException>());
		}
	}
}


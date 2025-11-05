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
	public class ExtenderReservaStockUseCaseTests
	{
		[Test]
		public async Task Handle_ExtiendeVigencia_CommitUnaVez()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-E1");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();
			var prodId = ProductoId.New();
			var nuevaFecha = DateTimeOffset.UtcNow.AddDays(3);

			var tenant = new FakeTenantContext(empresaId);
			var uow = new FakeUnitOfWork();
			var reservaRepo = new FakeReservaStockRepository();

			var reserva = ReservaStock.Crear(empresaId, estId, almId, prodId, CantidadStock.From(2m), DateTimeOffset.UtcNow.AddDays(1));
			reservaRepo.Add(reserva);

			var sut = new ExtenderReservaStockUseCase(reservaRepo, tenant, uow);
			var req = new ExtenderReservaStockUseCase.Request(estId.Value, almId.Value, reserva.ReservaId, nuevaFecha);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.Ok, Is.True);
			var recargada = await reservaRepo.ObtenerAsync(empresaId, estId, almId, reserva.ReservaId, ct);
			Assert.That(recargada!.VenceEn, Is.EqualTo(nuevaFecha).Within(TimeSpan.FromSeconds(1)));
			Assert.That(uow.CommitCalls, Is.EqualTo(1));
		}

		[Test]
		public void Handle_ReservaInexistente_LanzaNotFound_SinCommit()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-E2");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();
			var tenant = new FakeTenantContext(empresaId);
			var uow = new FakeUnitOfWork();
			var reservaRepo = new FakeReservaStockRepository();

			var sut = new ExtenderReservaStockUseCase(reservaRepo, tenant, uow);
			var req = new ExtenderReservaStockUseCase.Request(estId.Value, almId.Value, Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(2));
			var ct = CancellationToken.None;

			// Act + Assert
			Assert.That(() => sut.Handle(req, ct), Throws.TypeOf<NotFoundException>());
			Assert.That(uow.CommitCalls, Is.EqualTo(0));
		}

		[Test]
		public void Handle_NuevaFechaInvalida_LanzaBusinessRule_SinCommit()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-E3");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();
			var prodId = ProductoId.New();
			var tenant = new FakeTenantContext(empresaId);
			var uow = new FakeUnitOfWork();
			var reservaRepo = new FakeReservaStockRepository();

			var reserva = ReservaStock.Crear(empresaId, estId, almId, prodId, CantidadStock.From(1m), DateTimeOffset.UtcNow.AddDays(1));
			reservaRepo.Add(reserva);

			var sut = new ExtenderReservaStockUseCase(reservaRepo, tenant, uow);
			// nueva fecha <= ahora
			var req = new ExtenderReservaStockUseCase.Request(estId.Value, almId.Value, reserva.ReservaId, DateTimeOffset.UtcNow.AddSeconds(-1));
			var ct = CancellationToken.None;

			// Act + Assert
			Assert.That(() => sut.Handle(req, ct), Throws.TypeOf<BusinessRuleException>());
			Assert.That(uow.CommitCalls, Is.EqualTo(0));
		}
	}
}


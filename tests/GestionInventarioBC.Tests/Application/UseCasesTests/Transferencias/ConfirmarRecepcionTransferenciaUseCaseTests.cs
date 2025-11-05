using System;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.UseCases.Transferencias;
using GestionInventarioBC.Domain.Aggregates;
using GestionInventarioBC.Domain.ValueObjects;
using GestionInventarioBC.Tests.TestUtils;
using NUnit.Framework;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Tests.Application.UseCasesTests.Transferencias
{
	[TestFixture]
	public class ConfirmarRecepcionTransferenciaUseCaseTests
	{
		[Test]
		public async Task Handle_Confirma_AjustaStockYEstado_CommitUnaVez()
		{
			// Arrange
			var empresa = new EmpresaId("EMP-CRT-1");
			var origenEst = EstablecimientoId.New();
			var origenAlm = AlmacenId.New();
			var destinoEst = EstablecimientoId.New();
			var destinoAlm = AlmacenId.New();
			var pid = ProductoId.New();

			var tenant = new FakeTenantContext(empresa);
			var uow = new FakeUnitOfWork();
			var stockRepo = new FakeStockPorAlmacenRepository();
			var tRepo = new FakeTransferenciaInventarioRepository();

			// Stock origen suficiente
			stockRepo.Ensure(empresa, origenEst, origenAlm, pid, real: 10m);

			// Transferencia creada
			var t = TransferenciaInventario.Crear(empresa, origenEst, origenAlm, destinoEst, destinoAlm, pid, CantidadStock.From(4m));
			tRepo.Add(t);

			var sut = new ConfirmarRecepcionTransferenciaUseCase(tRepo, stockRepo, tenant, uow);
			var req = new ConfirmarRecepcionTransferenciaUseCase.Request(t.TransferenciaId);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.Ok, Is.True);
			var origen = await stockRepo.ObtenerAsync(empresa, origenEst, origenAlm, pid, ct);
			var destino = await stockRepo.ObtenerAsync(empresa, destinoEst, destinoAlm, pid, ct);
			Assert.That(origen!.Real.Value, Is.EqualTo(6m)); // 10 - 4
			Assert.That(destino!.Real.Value, Is.EqualTo(4m));
			var actualizado = await tRepo.ObtenerAsync(empresa, t.TransferenciaId, ct);
			Assert.That(actualizado!.Estado, Is.EqualTo(EstadoTransferencia.Confirmada));
			Assert.That(uow.CommitCalls, Is.EqualTo(1));
		}

		[Test]
		public void Handle_NotFound_LanzaNotFound_SinCommit()
		{
			// Arrange
			var empresa = new EmpresaId("EMP-CRT-2");
			var tenant = new FakeTenantContext(empresa);
			var uow = new FakeUnitOfWork();
			var stockRepo = new FakeStockPorAlmacenRepository();
			var tRepo = new FakeTransferenciaInventarioRepository();
			var sut = new ConfirmarRecepcionTransferenciaUseCase(tRepo, stockRepo, tenant, uow);
			var req = new ConfirmarRecepcionTransferenciaUseCase.Request(Guid.NewGuid());
			var ct = CancellationToken.None;

			// Act + Assert
			Assert.That(() => sut.Handle(req, ct), Throws.TypeOf<NotFoundException>());
			Assert.That(uow.CommitCalls, Is.EqualTo(0));
		}

		[Test]
		public void Handle_DisponibilidadInsuficienteEnOrigen_LanzaBusinessRule_SinCommit()
		{
			// Arrange
			var empresa = new EmpresaId("EMP-CRT-3");
			var origenEst = EstablecimientoId.New();
			var origenAlm = AlmacenId.New();
			var destinoEst = EstablecimientoId.New();
			var destinoAlm = AlmacenId.New();
			var pid = ProductoId.New();

			var tenant = new FakeTenantContext(empresa);
			var uow = new FakeUnitOfWork();
			var stockRepo = new FakeStockPorAlmacenRepository();
			var tRepo = new FakeTransferenciaInventarioRepository();

			// Stock origen insuficiente (1)
			stockRepo.Ensure(empresa, origenEst, origenAlm, pid, real: 1m);
			var t = TransferenciaInventario.Crear(empresa, origenEst, origenAlm, destinoEst, destinoAlm, pid, CantidadStock.From(5m));
			tRepo.Add(t);

			var sut = new ConfirmarRecepcionTransferenciaUseCase(tRepo, stockRepo, tenant, uow);
			var req = new ConfirmarRecepcionTransferenciaUseCase.Request(t.TransferenciaId);
			var ct = CancellationToken.None;

			// Act + Assert
			Assert.That(() => sut.Handle(req, ct), Throws.TypeOf<BusinessRuleException>());
			Assert.That(uow.CommitCalls, Is.EqualTo(0));
		}

		[Test]
		public async Task Handle_DobleConfirmacion_SegundaFalla_SinCommitExtra()
		{
			// Nota: la implementación actual no es idempotente en efectos antes de confirmar,
			// pero sí valida el estado al confirmar. Verificamos que la segunda invocación falla y no hace commit adicional.
			// Arrange
			var empresa = new EmpresaId("EMP-CRT-4");
			var origenEst = EstablecimientoId.New();
			var origenAlm = AlmacenId.New();
			var destinoEst = EstablecimientoId.New();
			var destinoAlm = AlmacenId.New();
			var pid = ProductoId.New();

			var tenant = new FakeTenantContext(empresa);
			var uow = new FakeUnitOfWork();
			var stockRepo = new FakeStockPorAlmacenRepository();
			var tRepo = new FakeTransferenciaInventarioRepository();
			stockRepo.Ensure(empresa, origenEst, origenAlm, pid, real: 20m);
			var t = TransferenciaInventario.Crear(empresa, origenEst, origenAlm, destinoEst, destinoAlm, pid, CantidadStock.From(5m));
			tRepo.Add(t);

			var sut = new ConfirmarRecepcionTransferenciaUseCase(tRepo, stockRepo, tenant, uow);
			var req = new ConfirmarRecepcionTransferenciaUseCase.Request(t.TransferenciaId);
			var ct = CancellationToken.None;

			// Act
			var r1 = await sut.Handle(req, ct);
			Assert.That(r1.Ok, Is.True);
			Assert.That(uow.CommitCalls, Is.EqualTo(1));

			// Segunda confirmación debe fallar por estado
			Assert.That(() => sut.Handle(req, ct), Throws.TypeOf<BusinessRuleException>());
			Assert.That(uow.CommitCalls, Is.EqualTo(1));
		}
	}
}


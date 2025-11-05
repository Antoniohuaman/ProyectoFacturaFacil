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
	public class CancelarTransferenciaPendienteUseCaseTests
	{
		[Test]
		public async Task Handle_Pendiente_Cancela_CommitUnaVez()
		{
			// Arrange
			var empresa = new EmpresaId("EMP-CAN-1");
			var estO = EstablecimientoId.New();
			var almO = AlmacenId.New();
			var estD = EstablecimientoId.New();
			var almD = AlmacenId.New();
			var pid = ProductoId.New();

			var tenant = new FakeTenantContext(empresa);
			var uow = new FakeUnitOfWork();
			var tRepo = new FakeTransferenciaInventarioRepository();
			var t = TransferenciaInventario.Crear(empresa, estO, almO, estD, almD, pid, CantidadStock.From(1m));
			tRepo.Add(t);

			var sut = new CancelarTransferenciaPendienteUseCase(tRepo, tenant, uow);
			var req = new CancelarTransferenciaPendienteUseCase.Request(t.TransferenciaId);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.Ok, Is.True);
			var t2 = await tRepo.ObtenerAsync(empresa, t.TransferenciaId, ct);
			Assert.That(t2!.Estado, Is.EqualTo(EstadoTransferencia.Cancelada));
			Assert.That(uow.CommitCalls, Is.EqualTo(1));
		}

		[Test]
		public void Handle_Confirmada_NoSePuedeCancelar_BusinessRule_SinCommit()
		{
			// Arrange
			var empresa = new EmpresaId("EMP-CAN-2");
			var estO = EstablecimientoId.New();
			var almO = AlmacenId.New();
			var estD = EstablecimientoId.New();
			var almD = AlmacenId.New();
			var pid = ProductoId.New();
			var tenant = new FakeTenantContext(empresa);
			var uow = new FakeUnitOfWork();
			var tRepo = new FakeTransferenciaInventarioRepository();
			var t = TransferenciaInventario.Crear(empresa, estO, almO, estD, almD, pid, CantidadStock.From(1m));
			t.Confirmar();
			tRepo.Add(t);

			var sut = new CancelarTransferenciaPendienteUseCase(tRepo, tenant, uow);
			var req = new CancelarTransferenciaPendienteUseCase.Request(t.TransferenciaId);
			var ct = CancellationToken.None;

			// Act + Assert
			Assert.That(() => sut.Handle(req, ct), Throws.TypeOf<BusinessRuleException>());
			Assert.That(uow.CommitCalls, Is.EqualTo(0));
		}

		[Test]
		public void Handle_NotFound_LanzaNotFound_SinCommit()
		{
			// Arrange
			var empresa = new EmpresaId("EMP-CAN-3");
			var tenant = new FakeTenantContext(empresa);
			var uow = new FakeUnitOfWork();
			var tRepo = new FakeTransferenciaInventarioRepository();
			var sut = new CancelarTransferenciaPendienteUseCase(tRepo, tenant, uow);
			var req = new CancelarTransferenciaPendienteUseCase.Request(Guid.NewGuid());
			var ct = CancellationToken.None;

			// Act + Assert
			Assert.That(() => sut.Handle(req, ct), Throws.TypeOf<NotFoundException>());
			Assert.That(uow.CommitCalls, Is.EqualTo(0));
		}
	}
}


using System;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.UseCases.Transferencias;
using GestionInventarioBC.Domain.ValueObjects;
using GestionInventarioBC.Tests.TestUtils;
using NUnit.Framework;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Tests.Application.UseCasesTests.Transferencias
{
	[TestFixture]
	public class CrearTransferenciaPendienteUseCaseTests
	{
		[Test]
		public async Task Handle_ProductoIdDirecto_CreaPendiente_CommitUnaVez()
		{
			// Arrange
			var empresa = new EmpresaId("EMP-T1");
			var origenEst = EstablecimientoId.New();
			var origenAlm = AlmacenId.New();
			var destinoEst = EstablecimientoId.New();
			var destinoAlm = AlmacenId.New();
			var pid = ProductoId.New();

			var tenant = new FakeTenantContext(empresa);
			var uow = new FakeUnitOfWork();
			var catalogo = new FakeCatalogoReadModel();
			var tRepo = new FakeTransferenciaInventarioRepository();

			var sut = new CrearTransferenciaPendienteUseCase(tRepo, catalogo, tenant, uow);
			var req = new CrearTransferenciaPendienteUseCase.Request(origenEst.Value, origenAlm.Value, destinoEst.Value, destinoAlm.Value, Sku: null, ProductoId: pid.Value, Cantidad: 5m);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.TransferenciaId, Is.Not.EqualTo(Guid.Empty));
			var t = await tRepo.ObtenerAsync(empresa, res.TransferenciaId, ct);
			Assert.That(t, Is.Not.Null);
			Assert.That(t!.Estado, Is.EqualTo(EstadoTransferencia.Creada));
			Assert.That(uow.CommitCalls, Is.EqualTo(1));
		}

		[Test]
		public async Task Handle_SoloSku_MapeaYCreacionPendiente()
		{
			// Arrange
			var empresa = new EmpresaId("EMP-T2");
			var origenEst = EstablecimientoId.New();
			var origenAlm = AlmacenId.New();
			var destinoEst = EstablecimientoId.New();
			var destinoAlm = AlmacenId.New();
			var pid = ProductoId.New();

			var tenant = new FakeTenantContext(empresa);
			var uow = new FakeUnitOfWork();
			var catalogo = new FakeCatalogoReadModel();
			var tRepo = new FakeTransferenciaInventarioRepository();
			catalogo.Seed(empresa.Value, "SKU-1", pid, "Prod 1");

			var sut = new CrearTransferenciaPendienteUseCase(tRepo, catalogo, tenant, uow);
			var req = new CrearTransferenciaPendienteUseCase.Request(origenEst.Value, origenAlm.Value, destinoEst.Value, destinoAlm.Value, Sku: "SKU-1", ProductoId: null, Cantidad: 2m);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			var t = await tRepo.ObtenerAsync(empresa, res.TransferenciaId, ct);
			Assert.That(t, Is.Not.Null);
			Assert.That(t!.ProductoId, Is.EqualTo(pid));
			Assert.That(t.Estado, Is.EqualTo(EstadoTransferencia.Creada));
			Assert.That(uow.CommitCalls, Is.EqualTo(1));
		}

		[Test]
		public void Handle_OrigenIgualADestino_LanzaBusinessRule_SinCommit()
		{
			// Arrange
			var empresa = new EmpresaId("EMP-T3");
			var est = EstablecimientoId.New();
			var alm = AlmacenId.New();
			var pid = ProductoId.New();

			var tenant = new FakeTenantContext(empresa);
			var uow = new FakeUnitOfWork();
			var catalogo = new FakeCatalogoReadModel();
			var tRepo = new FakeTransferenciaInventarioRepository();

			var sut = new CrearTransferenciaPendienteUseCase(tRepo, catalogo, tenant, uow);
			var req = new CrearTransferenciaPendienteUseCase.Request(est.Value, alm.Value, est.Value, alm.Value, Sku: null, ProductoId: pid.Value, Cantidad: 1m);
			var ct = CancellationToken.None;

			// Act + Assert
			Assert.That(() => sut.Handle(req, ct), Throws.TypeOf<BusinessRuleException>());
			Assert.That(uow.CommitCalls, Is.EqualTo(0));
		}

		[Test]
		public void Handle_SkuYProductoIdInconsistentes_LanzaBusinessRule_SinCommit()
		{
			// Arrange
			var empresa = new EmpresaId("EMP-T4");
			var origenEst = EstablecimientoId.New();
			var origenAlm = AlmacenId.New();
			var destinoEst = EstablecimientoId.New();
			var destinoAlm = AlmacenId.New();
			var pidCatalogo = ProductoId.New();
			var pidSolicitado = ProductoId.New();

			var tenant = new FakeTenantContext(empresa);
			var uow = new FakeUnitOfWork();
			var catalogo = new FakeCatalogoReadModel();
			var tRepo = new FakeTransferenciaInventarioRepository();
			catalogo.Seed(empresa.Value, "SKU-X", pidCatalogo, "Prod X");

			var sut = new CrearTransferenciaPendienteUseCase(tRepo, catalogo, tenant, uow);
			var req = new CrearTransferenciaPendienteUseCase.Request(origenEst.Value, origenAlm.Value, destinoEst.Value, destinoAlm.Value, Sku: "SKU-X", ProductoId: pidSolicitado.Value, Cantidad: 1m);
			var ct = CancellationToken.None;

			// Act + Assert
			Assert.That(() => sut.Handle(req, ct), Throws.TypeOf<BusinessRuleException>());
			Assert.That(uow.CommitCalls, Is.EqualTo(0));
		}
	}
}


using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.UseCases.Consultas;
using GestionInventarioBC.Domain.Aggregates;
using GestionInventarioBC.Domain.ValueObjects;
using GestionInventarioBC.Tests.TestUtils;
using NUnit.Framework;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Tests.Application.UseCasesTests.Consultas
{
	[TestFixture]
	public class ConsultarTransferenciasPendientesUseCaseTests
	{
		[Test]
		public async Task Handle_PendientesPorAlmacen_DevuelveSoloCreadasPaginado()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-CTP-1");
			var estO = EstablecimientoId.New();
			var almO = AlmacenId.New();
			var estD = EstablecimientoId.New();
			var almD = AlmacenId.New();
			var pid = ProductoId.New();

			var tenant = new FakeTenantContext(empresaId);
			var catalogo = new FakeCatalogoReadModel();
			var tRepo = new FakeTransferenciaInventarioRepository();
			catalogo.Seed(empresaId.Value, "sku-t", pid, "Prod T");

			// 15 transferencias creadas (pendientes)
			for (int i = 0; i < 15; i++)
			{
				var t = TransferenciaInventario.Crear(empresaId, estO, almO, estD, almD, pid, CantidadStock.From(1m + i));
				tRepo.Add(t);
			}
			// Confirmada y cancelada no deben aparecer
			var tOk = TransferenciaInventario.Crear(empresaId, estO, almO, estD, almD, pid, CantidadStock.From(1m));
			tOk.Confirmar();
			tRepo.Add(tOk);
			var tCanc = TransferenciaInventario.Crear(empresaId, estO, almO, estD, almD, pid, CantidadStock.From(1m));
			tCanc.Cancelar();
			tRepo.Add(tCanc);

			var sut = new ConsultarTransferenciasPendientesUseCase(tRepo, tenant, catalogo);
			var req = new ConsultarTransferenciasPendientesUseCase.Request(OrigenEstablecimientoId: estO.Value, OrigenAlmacenId: almO.Value, DestinoEstablecimientoId: null, DestinoAlmacenId: null, Page: 2, PageSize: 10);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.Total, Is.EqualTo(15));
			Assert.That(res.Items.Count, Is.EqualTo(5));
			Assert.That(res.Items.All(i => i.Sku == "sku-t" && i.Nombre == "Prod T"), Is.True);
		}

		[Test]
		public async Task Handle_ExcluyeConfirmadasYCanceladas()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-CTP-2");
			var estO = EstablecimientoId.New();
			var almO = AlmacenId.New();
			var estD = EstablecimientoId.New();
			var almD = AlmacenId.New();
			var pid = ProductoId.New();

			var tenant = new FakeTenantContext(empresaId);
			var catalogo = new FakeCatalogoReadModel();
			var tRepo = new FakeTransferenciaInventarioRepository();
			catalogo.Seed(empresaId.Value, "sku", pid, "N");

			var t1 = TransferenciaInventario.Crear(empresaId, estO, almO, estD, almD, pid, CantidadStock.From(5m)); // creada
			var t2 = TransferenciaInventario.Crear(empresaId, estO, almO, estD, almD, pid, CantidadStock.From(2m)); t2.Confirmar();
			var t3 = TransferenciaInventario.Crear(empresaId, estO, almO, estD, almD, pid, CantidadStock.From(1m)); t3.Cancelar();
			tRepo.Add(t1); tRepo.Add(t2); tRepo.Add(t3);

			var sut = new ConsultarTransferenciasPendientesUseCase(tRepo, tenant, catalogo);
			var req = new ConsultarTransferenciasPendientesUseCase.Request(estO.Value, almO.Value, null, null, Page: 1, PageSize: 10);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert: solo una creada
			Assert.That(res.Total, Is.EqualTo(1));
			Assert.That(res.Items.Count, Is.EqualTo(1));
		}

		[Test]
		public async Task Handle_AislamientoTenantYAlmacen_NoContaminaResultados()
		{
			// Arrange
			var empresaA = new EmpresaId("EMP-A");
			var empresaB = new EmpresaId("EMP-B");
			var estO = EstablecimientoId.New();
			var almO = AlmacenId.New();
			var estD = EstablecimientoId.New();
			var almD = AlmacenId.New();
			var pid = ProductoId.New();

			var tenantA = new FakeTenantContext(empresaA);
			var catalogo = new FakeCatalogoReadModel();
			var tRepo = new FakeTransferenciaInventarioRepository();
			catalogo.Seed(empresaA.Value, "sku", pid, "N");
			catalogo.Seed(empresaB.Value, "sku", pid, "N B");

			// empresa A creada
			tRepo.Add(TransferenciaInventario.Crear(empresaA, estO, almO, estD, almD, pid, CantidadStock.From(1m)));
			// empresa B creada (no debe verse)
			tRepo.Add(TransferenciaInventario.Crear(empresaB, estO, almO, estD, almD, pid, CantidadStock.From(1m)));

			var sut = new ConsultarTransferenciasPendientesUseCase(tRepo, tenantA, catalogo);
			var req = new ConsultarTransferenciasPendientesUseCase.Request(estO.Value, almO.Value, null, null, Page: 1, PageSize: 10);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.Total, Is.EqualTo(1));
			Assert.That(res.Items.Count, Is.EqualTo(1));
			Assert.That(res.Items[0].Sku, Is.EqualTo("sku"));
		}
	}
}


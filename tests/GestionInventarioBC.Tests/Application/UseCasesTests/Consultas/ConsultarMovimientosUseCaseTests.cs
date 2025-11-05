using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.UseCases.Consultas;
using GestionInventarioBC.Domain.Aggregates;
using GestionInventarioBC.Domain.Entities;
using GestionInventarioBC.Domain.ValueObjects;
using GestionInventarioBC.Tests.TestUtils;
using NUnit.Framework;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Tests.Application.UseCasesTests.Consultas
{
	[TestFixture]
	public class ConsultarMovimientosUseCaseTests
	{
		private static MovimientoInventario Mov(EmpresaId emp, EstablecimientoId est, AlmacenId alm, DateTimeOffset fecha, TipoMovimiento tipo, MotivoMovimiento motivo, params (ProductoId pid, decimal cant)[] lineas)
		{
			var ls = new List<LineaMovimiento>();
			foreach (var (pid, cant) in lineas)
			{
				ls.Add(LineaMovimiento.Crear(pid, CantidadStock.From(cant)));
			}
			return MovimientoInventario.Registrar(emp, est, alm, fecha, tipo, motivo, ls);
		}

		[Test]
		public async Task Handle_RangoFechasYOrdenDesc_RetornaSoloEnRangoYOrdenado()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-CM-1");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();
			var pid = ProductoId.New();

			var tenant = new FakeTenantContext(empresaId);
			var catalogo = new FakeCatalogoReadModel();
			var movRepo = new FakeMovimientoInventarioRepository();

			catalogo.Seed(empresaId.Value, "sku-1", pid, "P1");

			var f1 = DateTimeOffset.UtcNow.AddDays(-3);
			var f2 = DateTimeOffset.UtcNow.AddDays(-2);
			var f3 = DateTimeOffset.UtcNow.AddDays(-1);
			await movRepo.GuardarAsync(Mov(empresaId, estId, almId, f1, TipoMovimiento.Ingreso, MotivoMovimiento.Compra, (pid, 5m)));
			await movRepo.GuardarAsync(Mov(empresaId, estId, almId, f2, TipoMovimiento.Egreso, MotivoMovimiento.Venta, (pid, 2m)));
			await movRepo.GuardarAsync(Mov(empresaId, estId, almId, f3, TipoMovimiento.AjustePositivo, MotivoMovimiento.Ajuste, (pid, 1m)));

			var sut = new ConsultarMovimientosUseCase(movRepo, tenant, catalogo);
			var req = new ConsultarMovimientosUseCase.Request(estId.Value, almId.Value, Desde: f2.AddHours(-1), Hasta: f3.AddHours(1), Sku: null, Tipo: null, Motivo: null, Page: 1, PageSize: 50);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert: solo f2 y f3 en orden desc
			Assert.That(res.Total, Is.EqualTo(2));
			Assert.That(res.Items.Count, Is.EqualTo(2));
			Assert.That(res.Items[0].Fecha, Is.GreaterThan(res.Items[1].Fecha));
		}

		[Test]
		public async Task Handle_Paginacion_TotalEItemsCoherentes()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-CM-2");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();
			var pid = ProductoId.New();

			var tenant = new FakeTenantContext(empresaId);
			var catalogo = new FakeCatalogoReadModel();
			var movRepo = new FakeMovimientoInventarioRepository();
			catalogo.Seed(empresaId.Value, "sku", pid, "N");

			var baseFecha = DateTimeOffset.UtcNow.AddDays(-10);
			for (int i = 0; i < 30; i++)
			{
				await movRepo.GuardarAsync(Mov(empresaId, estId, almId, baseFecha.AddDays(i), TipoMovimiento.Ingreso, MotivoMovimiento.Compra, (pid, 1m)));
			}
			var sut = new ConsultarMovimientosUseCase(movRepo, tenant, catalogo);
			var req = new ConsultarMovimientosUseCase.Request(estId.Value, almId.Value, null, null, null, null, null, Page: 2, PageSize: 10);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.Total, Is.EqualTo(30));
			Assert.That(res.Items.Count, Is.EqualTo(10));
		}

		[Test]
		public async Task Handle_FiltroPorSku_DevuelveSoloMovimientosDelProducto()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-CM-3");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();
			var pid1 = ProductoId.New();
			var pid2 = ProductoId.New();

			var tenant = new FakeTenantContext(empresaId);
			var catalogo = new FakeCatalogoReadModel();
			var movRepo = new FakeMovimientoInventarioRepository();
			catalogo.Seed(empresaId.Value, "sku-1", pid1, "P1");
			catalogo.Seed(empresaId.Value, "sku-2", pid2, "P2");

			await movRepo.GuardarAsync(Mov(empresaId, estId, almId, DateTimeOffset.UtcNow.AddDays(-3), TipoMovimiento.Ingreso, MotivoMovimiento.Compra, (pid1, 3m)));
			await movRepo.GuardarAsync(Mov(empresaId, estId, almId, DateTimeOffset.UtcNow.AddDays(-2), TipoMovimiento.Egreso, MotivoMovimiento.Venta, (pid2, 1m)));
			await movRepo.GuardarAsync(Mov(empresaId, estId, almId, DateTimeOffset.UtcNow.AddDays(-1), TipoMovimiento.Ingreso, MotivoMovimiento.Compra, (pid1, 2m)));

			var sut = new ConsultarMovimientosUseCase(movRepo, tenant, catalogo);
			var req = new ConsultarMovimientosUseCase.Request(estId.Value, almId.Value, null, null, Sku: "sku-1", Tipo: null, Motivo: null, Page: 1, PageSize: 50);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert: solo movimientos con líneas de pid1
			Assert.That(res.Items.All(x => x.Lineas.All(l => l.Sku == "sku-1")), Is.True);
		}

		[Test]
		public async Task Handle_SinResultados_ListaVaciaYTotalCero()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-CM-4");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();
			var tenant = new FakeTenantContext(empresaId);
			var catalogo = new FakeCatalogoReadModel();
			var movRepo = new FakeMovimientoInventarioRepository();
			var sut = new ConsultarMovimientosUseCase(movRepo, tenant, catalogo);
			var req = new ConsultarMovimientosUseCase.Request(estId.Value, almId.Value, null, null, null, null, null, Page: 1, PageSize: 10);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.Total, Is.EqualTo(0));
			Assert.That(res.Items.Count, Is.EqualTo(0));
		}
	}
}


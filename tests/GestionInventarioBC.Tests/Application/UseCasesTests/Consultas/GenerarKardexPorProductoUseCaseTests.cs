using System;
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
	public class GenerarKardexPorProductoUseCaseTests
	{
		private static MovimientoInventario Mov(EmpresaId emp, EstablecimientoId est, AlmacenId alm, DateTimeOffset fecha, TipoMovimiento tipo, MotivoMovimiento motivo, ProductoId pid, decimal cant)
		{
			var linea = LineaMovimiento.Crear(pid, CantidadStock.From(cant));
			return MovimientoInventario.Registrar(emp, est, alm, fecha, tipo, motivo, new[] { linea });
		}

		[Test]
		public async Task Handle_SecuenciaMixta_GeneraSaldosAcumuladosCorrectos()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-GK-1");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();
			var pid = ProductoId.New();
			var sku = "sku-k";

			var tenant = new FakeTenantContext(empresaId);
			var catalogo = new FakeCatalogoReadModel();
			var movRepo = new FakeMovimientoInventarioRepository();
			catalogo.Seed(empresaId.Value, sku, pid, "Producto K");

			var f1 = DateTimeOffset.UtcNow.AddDays(-3);
			var f2 = DateTimeOffset.UtcNow.AddDays(-2);
			var f3 = DateTimeOffset.UtcNow.AddDays(-1);
			await movRepo.GuardarAsync(Mov(empresaId, estId, almId, f1, TipoMovimiento.Ingreso, MotivoMovimiento.Compra, pid, 10m)); // saldo 10
			await movRepo.GuardarAsync(Mov(empresaId, estId, almId, f2, TipoMovimiento.Egreso, MotivoMovimiento.Venta, pid, 3m));   // saldo 7
			await movRepo.GuardarAsync(Mov(empresaId, estId, almId, f3, TipoMovimiento.AjustePositivo, MotivoMovimiento.Ajuste, pid, 2m)); // saldo 9

			var sut = new GenerarKardexPorProductoUseCase(movRepo, tenant, catalogo);
			var req = new GenerarKardexPorProductoUseCase.Request(estId.Value, almId.Value, sku, Desde: null, Hasta: null, Page: 1, PageSize: 50);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert
			Assert.That(res.Total, Is.EqualTo(3));
			Assert.That(res.Items[0].SaldoAcumulado, Is.EqualTo(10m));
			Assert.That(res.Items[1].SaldoAcumulado, Is.EqualTo(7m));
			Assert.That(res.Items[2].SaldoAcumulado, Is.EqualTo(9m));
		}

		[Test]
		public async Task Handle_RangoDeFechas_IncluyeSoloDentroDelRango()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-GK-2");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();
			var pid = ProductoId.New();
			var sku = "sku-r";

			var tenant = new FakeTenantContext(empresaId);
			var catalogo = new FakeCatalogoReadModel();
			var movRepo = new FakeMovimientoInventarioRepository();
			catalogo.Seed(empresaId.Value, sku, pid, "Rango");

			var f1 = DateTimeOffset.UtcNow.AddDays(-5);
			var f2 = DateTimeOffset.UtcNow.AddDays(-3);
			var f3 = DateTimeOffset.UtcNow.AddDays(-1);
			await movRepo.GuardarAsync(Mov(empresaId, estId, almId, f1, TipoMovimiento.Ingreso, MotivoMovimiento.Compra, pid, 1m));
			await movRepo.GuardarAsync(Mov(empresaId, estId, almId, f2, TipoMovimiento.Ingreso, MotivoMovimiento.Compra, pid, 1m));
			await movRepo.GuardarAsync(Mov(empresaId, estId, almId, f3, TipoMovimiento.Ingreso, MotivoMovimiento.Compra, pid, 1m));

			var sut = new GenerarKardexPorProductoUseCase(movRepo, tenant, catalogo);
			var req = new GenerarKardexPorProductoUseCase.Request(estId.Value, almId.Value, sku, Desde: f2.AddHours(-1), Hasta: f3.AddHours(-1), Page: 1, PageSize: 50);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert: solo f2 dentro del rango
			Assert.That(res.Total, Is.EqualTo(1));
			Assert.That(res.Items[0].Entrada, Is.EqualTo(1m));
			Assert.That(res.Items[0].Salida, Is.EqualTo(0m));
		}

		[Test]
		public void Handle_ProductoInexistente_LanzaInvalidOperation()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-GK-3");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();
			var tenant = new FakeTenantContext(empresaId);
			var catalogo = new FakeCatalogoReadModel(); // no se seed-ea sku
			var movRepo = new FakeMovimientoInventarioRepository();
			var sut = new GenerarKardexPorProductoUseCase(movRepo, tenant, catalogo);
			var req = new GenerarKardexPorProductoUseCase.Request(estId.Value, almId.Value, "no-existe", Desde: null, Hasta: null, Page: 1, PageSize: 50);
			var ct = CancellationToken.None;

			// Act + Assert
			Assert.That(() => sut.Handle(req, ct), Throws.TypeOf<InvalidOperationException>());
		}

		[Test]
		public async Task Handle_MetodoValoracionSimple_AcumulaPorTipoMovimiento()
		{
			// Arrange
			var empresaId = new EmpresaId("EMP-GK-4");
			var estId = EstablecimientoId.New();
			var almId = AlmacenId.New();
			var pid = ProductoId.New();
			var sku = "sku-mv";

			var tenant = new FakeTenantContext(empresaId);
			var catalogo = new FakeCatalogoReadModel();
			var movRepo = new FakeMovimientoInventarioRepository();
			catalogo.Seed(empresaId.Value, sku, pid, "MV");

			var baseF = DateTimeOffset.UtcNow.AddDays(-4);
			await movRepo.GuardarAsync(Mov(empresaId, estId, almId, baseF.AddDays(0), TipoMovimiento.Ingreso, MotivoMovimiento.Compra, pid, 5m));  // saldo 5
			await movRepo.GuardarAsync(Mov(empresaId, estId, almId, baseF.AddDays(1), TipoMovimiento.AjusteNegativo, MotivoMovimiento.Ajuste, pid, 2m)); // saldo 3
			await movRepo.GuardarAsync(Mov(empresaId, estId, almId, baseF.AddDays(2), TipoMovimiento.TransferenciaEntrada, MotivoMovimiento.Transferencia, pid, 10m)); // saldo 13
			await movRepo.GuardarAsync(Mov(empresaId, estId, almId, baseF.AddDays(3), TipoMovimiento.TransferenciaSalida, MotivoMovimiento.Transferencia, pid, 4m)); // saldo 9

			var sut = new GenerarKardexPorProductoUseCase(movRepo, tenant, catalogo);
			var req = new GenerarKardexPorProductoUseCase.Request(estId.Value, almId.Value, sku, Desde: null, Hasta: null, Page: 1, PageSize: 100);
			var ct = CancellationToken.None;

			// Act
			var res = await sut.Handle(req, ct);

			// Assert (verificamos acumulados básicos según tipo)
			Assert.That(res.Total, Is.EqualTo(4));
			Assert.That(res.Items[0].SaldoAcumulado, Is.EqualTo(5m));
			Assert.That(res.Items[1].SaldoAcumulado, Is.EqualTo(3m));
			Assert.That(res.Items[2].SaldoAcumulado, Is.EqualTo(13m));
			Assert.That(res.Items[3].SaldoAcumulado, Is.EqualTo(9m));
		}
	}
}


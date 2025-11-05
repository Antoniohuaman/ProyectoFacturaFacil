using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.UseCases.Movimientos;
using GestionInventarioBC.Domain.Aggregates;
using GestionInventarioBC.Domain.Entities;
using GestionInventarioBC.Domain.ValueObjects;
using GestionInventarioBC.Tests.TestUtils;
using NUnit.Framework;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Tests.Application.UseCasesTests.Movimientos
{
    [TestFixture]
    public class AnularMovimientoInventarioUseCaseTests
    {
        private readonly CancellationToken _ct = CancellationToken.None;

        [Test]
        public async Task AnulaIngreso_DisminuyeStock_YCreaCompensatorio()
        {
            // Arrange
            var emp = EmpresaId.From("emp-test");
            var est = EstablecimientoId.From(Guid.NewGuid());
            var alm = AlmacenId.From(Guid.NewGuid());
            var tenant = new FakeTenantContext(emp);
            var uow = new FakeUnitOfWork();
            var movRepo = new FakeMovimientoInventarioRepository();
            var stockRepo = new FakeStockPorAlmacenRepository();

            var prod = ProductoId.From(Guid.NewGuid());
            stockRepo.Ensure(emp, est, alm, prod, 5m);

            // Seed original ingreso de 2
            var lineas = new List<LineaMovimiento> { LineaMovimiento.Crear(prod, CantidadStock.From(2m)) };
            var original = MovimientoInventario.Registrar(emp, est, alm, DateTimeOffset.UtcNow.AddMinutes(-10), TipoMovimiento.Ingreso, MotivoMovimiento.Compra, lineas);
            await movRepo.GuardarAsync(original, _ct);

            var useCase = new AnularMovimientoInventarioUseCase(movRepo, stockRepo, tenant, uow);
            var req = new AnularMovimientoInventarioUseCase.Request(est.Value, alm.Value, original.MovimientoId, DateTimeOffset.UtcNow);

            // Act
            var resp = await useCase.Handle(req, _ct);

            // Assert
            Assert.That(resp.MovimientoCompensatorioId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(uow.CommitCalls, Is.EqualTo(1));
            var s = await stockRepo.ObtenerAsync(emp, est, alm, prod, _ct);
            Assert.That(s!.Real.Value, Is.EqualTo(3m));
        }

        [Test]
        public void FallaSiMovimientoNoExiste()
        {
            // Arrange
            var emp = EmpresaId.From("emp-test");
            var est = EstablecimientoId.From(Guid.NewGuid());
            var alm = AlmacenId.From(Guid.NewGuid());
            var tenant = new FakeTenantContext(emp);
            var uow = new FakeUnitOfWork();
            var movRepo = new FakeMovimientoInventarioRepository();
            var stockRepo = new FakeStockPorAlmacenRepository();

            var useCase = new AnularMovimientoInventarioUseCase(movRepo, stockRepo, tenant, uow);
            var req = new AnularMovimientoInventarioUseCase.Request(est.Value, alm.Value, Guid.NewGuid(), DateTimeOffset.UtcNow);

            // Act & Assert
            Assert.That(async () => await useCase.Handle(req, _ct), Throws.TypeOf<NotFoundException>());
            Assert.That(uow.CommitCalls, Is.EqualTo(0));
        }

        [Test]
        public async Task AnulaEgreso_AumentaStock_CreandoSiNoExistia()
        {
            // Arrange
            var emp = EmpresaId.From("emp-test");
            var est = EstablecimientoId.From(Guid.NewGuid());
            var alm = AlmacenId.From(Guid.NewGuid());
            var tenant = new FakeTenantContext(emp);
            var uow = new FakeUnitOfWork();
            var movRepo = new FakeMovimientoInventarioRepository();
            var stockRepo = new FakeStockPorAlmacenRepository();

            var prod = ProductoId.From(Guid.NewGuid());
            // No seed stock -> repo will crear nuevo al anular
            var lineas = new List<LineaMovimiento> { LineaMovimiento.Crear(prod, CantidadStock.From(3m)) };
            var original = MovimientoInventario.Registrar(emp, est, alm, DateTimeOffset.UtcNow.AddMinutes(-5), TipoMovimiento.Egreso, MotivoMovimiento.Venta, lineas);
            await movRepo.GuardarAsync(original, _ct);

            var useCase = new AnularMovimientoInventarioUseCase(movRepo, stockRepo, tenant, uow);
            var req = new AnularMovimientoInventarioUseCase.Request(est.Value, alm.Value, original.MovimientoId, DateTimeOffset.UtcNow);

            // Act
            var resp = await useCase.Handle(req, _ct);

            // Assert
            Assert.That(resp.MovimientoCompensatorioId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(uow.CommitCalls, Is.EqualTo(1));
            var s = await stockRepo.ObtenerAsync(emp, est, alm, prod, _ct);
            Assert.That(s!.Real.Value, Is.EqualTo(3m));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.UseCases.Movimientos;
using GestionInventarioBC.Domain.ValueObjects;
using GestionInventarioBC.Tests.TestUtils;
using NUnit.Framework;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Tests.Application.UseCasesTests.Movimientos
{
    [TestFixture]
    public class RegistrarSalidaUseCaseTests
    {
        private readonly CancellationToken _ct = CancellationToken.None;

        [Test]
        public async Task RegistraSalida_DisminuyeStockYCreaMovimiento()
        {
            // Arrange
            var emp = EmpresaId.From("emp-test");
            var est = EstablecimientoId.From(Guid.NewGuid());
            var alm = AlmacenId.From(Guid.NewGuid());
            var tenant = new FakeTenantContext(emp);
            var uow = new FakeUnitOfWork();
            var stockRepo = new FakeStockPorAlmacenRepository();
            var movRepo = new FakeMovimientoInventarioRepository();
            var catalogo = new FakeCatalogoReadModel();

            var prod = ProductoId.From(Guid.NewGuid());
            catalogo.Seed(emp.Value, "SKU-1", prod, "P1");
            // seed stock 10
            stockRepo.Ensure(emp, est, alm, prod, 10m);

            var useCase = new RegistrarSalidaUseCase(stockRepo, movRepo, tenant, uow, catalogo);
            var req = new RegistrarSalidaUseCase.Request(
                est.Value, alm.Value, DateTimeOffset.UtcNow, "Venta",
                new List<RegistrarSalidaUseCase.Linea> { new("SKU-1", null, 3m) }
            );

            // Act
            var resp = await useCase.Handle(req, _ct);

            // Assert
            Assert.That(resp.LineasAfectadas, Is.EqualTo(1));
            Assert.That(uow.CommitCalls, Is.EqualTo(1));
            var s = await stockRepo.ObtenerAsync(emp, est, alm, prod, _ct);
            Assert.That(s!.Real.Value, Is.EqualTo(7m));
        }

        [Test]
        public void FallaCuandoNoExisteStock()
        {
            // Arrange
            var emp = EmpresaId.From("emp-test");
            var est = EstablecimientoId.From(Guid.NewGuid());
            var alm = AlmacenId.From(Guid.NewGuid());
            var tenant = new FakeTenantContext(emp);
            var uow = new FakeUnitOfWork();
            var stockRepo = new FakeStockPorAlmacenRepository();
            var movRepo = new FakeMovimientoInventarioRepository();
            var catalogo = new FakeCatalogoReadModel();
            var prod = ProductoId.From(Guid.NewGuid());
            catalogo.Seed(emp.Value, "SKU-2", prod, "P2");

            var useCase = new RegistrarSalidaUseCase(stockRepo, movRepo, tenant, uow, catalogo);
            var req = new RegistrarSalidaUseCase.Request(
                est.Value, alm.Value, DateTimeOffset.UtcNow, "Venta",
                new List<RegistrarSalidaUseCase.Linea> { new("SKU-2", null, 1m) }
            );

            // Act & Assert
            Assert.That(async () => await useCase.Handle(req, _ct), Throws.TypeOf<NotFoundException>());
            Assert.That(uow.CommitCalls, Is.EqualTo(0));
        }

        [Test]
        public void FallaCuandoNoHayDisponibilidadSuficiente()
        {
            // Arrange
            var emp = EmpresaId.From("emp-test");
            var est = EstablecimientoId.From(Guid.NewGuid());
            var alm = AlmacenId.From(Guid.NewGuid());
            var tenant = new FakeTenantContext(emp);
            var uow = new FakeUnitOfWork();
            var stockRepo = new FakeStockPorAlmacenRepository();
            var movRepo = new FakeMovimientoInventarioRepository();
            var catalogo = new FakeCatalogoReadModel();
            var prod = ProductoId.From(Guid.NewGuid());
            catalogo.Seed(emp.Value, "SKU-3", prod, "P3");
            stockRepo.Ensure(emp, est, alm, prod, 2m);

            var useCase = new RegistrarSalidaUseCase(stockRepo, movRepo, tenant, uow, catalogo);
            var req = new RegistrarSalidaUseCase.Request(
                est.Value, alm.Value, DateTimeOffset.UtcNow, "Venta",
                new List<RegistrarSalidaUseCase.Linea> { new("SKU-3", null, 3m) }
            );

            // Act & Assert
            Assert.That(async () => await useCase.Handle(req, _ct), Throws.TypeOf<BusinessRuleException>());
            Assert.That(uow.CommitCalls, Is.EqualTo(0));
        }
    }
}

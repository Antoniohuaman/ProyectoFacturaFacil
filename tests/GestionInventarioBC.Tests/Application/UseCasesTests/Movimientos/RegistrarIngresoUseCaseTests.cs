using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.UseCases.Movimientos;
using GestionInventarioBC.Domain.ValueObjects;
using NUnit.Framework;
using SharedKernel.ValueObjects;
using SharedKernel.Exceptions;
using GestionInventarioBC.Tests.TestUtils;

namespace GestionInventarioBC.Tests.Application.UseCasesTests.Movimientos
{
    [TestFixture]
    public class RegistrarIngresoUseCaseTests
    {
        private readonly CancellationToken _ct = CancellationToken.None;

        [Test]
        public async Task RegistraIngreso_MultiplesLineas_IncrementaStockYCreaMovimiento()
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

            var prodA = ProductoId.From(Guid.NewGuid());
            var prodB = ProductoId.From(Guid.NewGuid());
            catalogo.Seed(emp.Value, "SKU-A", prodA, "Producto A");
            catalogo.Seed(emp.Value, "SKU-B", prodB, "Producto B");

            var useCase = new RegistrarIngresoUseCase(stockRepo, movRepo, tenant, uow, catalogo);
            var req = new RegistrarIngresoUseCase.Request(
                est.Value, alm.Value, DateTimeOffset.UtcNow, "Compra",
                new List<RegistrarIngresoUseCase.Linea>
                {
                    new RegistrarIngresoUseCase.Linea("SKU-A", null, 5m),
                    new RegistrarIngresoUseCase.Linea(null, prodB.Value, 2.5m)
                }
            );

            // Act
            var resp = await useCase.Handle(req, _ct);

            // Assert
            Assert.That(resp.LineasAfectadas, Is.EqualTo(2));
            Assert.That(uow.CommitCalls, Is.EqualTo(1));

            var sA = await stockRepo.ObtenerAsync(emp, est, alm, prodA, _ct);
            var sB = await stockRepo.ObtenerAsync(emp, est, alm, prodB, _ct);
            Assert.That(sA, Is.Not.Null);
            Assert.That(sA!.Real.Value, Is.EqualTo(5m));
            Assert.That(sB, Is.Not.Null);
            Assert.That(sB!.Real.Value, Is.EqualTo(2.5m));
        }

        [Test]
        public void FallaCuandoSkuNoExiste()
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

            var useCase = new RegistrarIngresoUseCase(stockRepo, movRepo, tenant, uow, catalogo);
            var req = new RegistrarIngresoUseCase.Request(
                est.Value, alm.Value, DateTimeOffset.UtcNow, "Compra",
                new List<RegistrarIngresoUseCase.Linea>
                {
                    new RegistrarIngresoUseCase.Linea("SKU-NO", null, 1m)
                }
            );

            // Act & Assert
            Assert.That(async () => await useCase.Handle(req, _ct), Throws.TypeOf<NotFoundException>());
            Assert.That(uow.CommitCalls, Is.EqualTo(0));
        }

        [Test]
        public void FallaCuandoNoHaySkuNiProductoId()
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

            var useCase = new RegistrarIngresoUseCase(stockRepo, movRepo, tenant, uow, catalogo);
            var req = new RegistrarIngresoUseCase.Request(
                est.Value, alm.Value, DateTimeOffset.UtcNow, "Compra",
                new List<RegistrarIngresoUseCase.Linea>
                {
                    new RegistrarIngresoUseCase.Linea(null, null, 1m)
                }
            );

            // Act & Assert
            Assert.That(async () => await useCase.Handle(req, _ct), Throws.TypeOf<ArgumentException>());
            Assert.That(uow.CommitCalls, Is.EqualTo(0));
        }
    }
}

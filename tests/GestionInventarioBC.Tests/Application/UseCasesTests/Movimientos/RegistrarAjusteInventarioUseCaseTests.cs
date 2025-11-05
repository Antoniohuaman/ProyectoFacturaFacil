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
    public class RegistrarAjusteInventarioUseCaseTests
    {
        private readonly CancellationToken _ct = CancellationToken.None;

        [Test]
        public async Task AjustePositivo_CreaMovimientoYActualizaStock()
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
            catalogo.Seed(emp.Value, "SKU-AJ", prod, "P AJ");
            stockRepo.Ensure(emp, est, alm, prod, 1m);

            var useCase = new RegistrarAjusteInventarioUseCase(stockRepo, movRepo, tenant, uow, catalogo);
            var req = new RegistrarAjusteInventarioUseCase.Request(
                est.Value, alm.Value, DateTimeOffset.UtcNow,
                new List<RegistrarAjusteInventarioUseCase.Item> { new("SKU-AJ", null, 4m, "Ajuste") }
            );

            // Act
            var resp = await useCase.Handle(req, _ct);

            // Assert
            Assert.That(resp.LineasAfectadas, Is.EqualTo(1));
            Assert.That(uow.CommitCalls, Is.EqualTo(1));
            var s = await stockRepo.ObtenerAsync(emp, est, alm, prod, _ct);
            Assert.That(s!.Real.Value, Is.EqualTo(5m));
        }

        [Test]
        public void AjusteTodosCero_Falla()
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
            catalogo.Seed(emp.Value, "SKU-Z", prod, "Z");

            var useCase = new RegistrarAjusteInventarioUseCase(stockRepo, movRepo, tenant, uow, catalogo);
            var req = new RegistrarAjusteInventarioUseCase.Request(
                est.Value, alm.Value, DateTimeOffset.UtcNow,
                new List<RegistrarAjusteInventarioUseCase.Item> { new("SKU-Z", null, 0m, "Ninguno") }
            );

            // Act & Assert
            Assert.That(async () => await useCase.Handle(req, _ct), Throws.TypeOf<BusinessRuleException>());
            Assert.That(uow.CommitCalls, Is.EqualTo(0));
        }

        [Test]
        public void AjusteNegativo_SinStockSuficiente_Falla()
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
            catalogo.Seed(emp.Value, "SKU-N", prod, "N");
            stockRepo.Ensure(emp, est, alm, prod, 1m);

            var useCase = new RegistrarAjusteInventarioUseCase(stockRepo, movRepo, tenant, uow, catalogo);
            var req = new RegistrarAjusteInventarioUseCase.Request(
                est.Value, alm.Value, DateTimeOffset.UtcNow,
                new List<RegistrarAjusteInventarioUseCase.Item> { new("SKU-N", null, -2m, "Ajuste") }
            );

            // Act & Assert
            Assert.That(async () => await useCase.Handle(req, _ct), Throws.TypeOf<BusinessRuleException>());
            Assert.That(uow.CommitCalls, Is.EqualTo(0));
        }
    }
}

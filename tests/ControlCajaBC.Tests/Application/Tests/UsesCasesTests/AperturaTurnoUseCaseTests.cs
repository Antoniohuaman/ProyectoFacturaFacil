using System;
using System.Threading.Tasks;
using NUnit.Framework; 
using ControlCajaBC.Adapters.Output.Persistence.InMemory;
using ControlCajaBC.Application.UseCases;
using ControlCajaBC.Domain.ValueObjects;
using ControlCajaBC.Domain.Entities;
using Moq;
using SharedKernel.Application.Interfaces;
using SharedKernel.ValueObjects;

namespace ControlCajaBC.Tests.UnitTests.UseCases
{
    public class AperturaTurnoUseCaseTests
    {
        [Test]
        public async Task Ejecutar_DeberíaAgregarTurnoYCometer()
        {
            // Arrange: instancio repositorio y UoW in-memory
            var repo = new InMemoryControlCajaRepository();
            var uow  = new InMemoryUnitOfWork();
            var tenant = new Mock<ITenantContext>(MockBehavior.Loose);
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("20123456789"));
            var useCase = new AperturaTurnoUseCase(repo, uow, tenant.Object);

            var codigo = CodigoCaja.New();
            var fecha  = new FechaHora(DateTime.UtcNow);
            var resp = new ResponsableCaja(Guid.NewGuid(), "Juan");
            var saldo  = new Monto(100m);

            // Act
            await useCase.HandleAsync(codigo, fecha, resp, saldo);

            // Assert: que el turno quedó almacenado
            var guardado = await repo.GetTurnoAbiertoAsync(codigo, tenant.Object.EmpresaId);
            Assert.That(guardado,           Is.Not.Null);
            Assert.That(guardado.CodigoCaja, Is.EqualTo(codigo));
            Assert.That(uow.WasCommitted,    Is.True);
        }
    }
}

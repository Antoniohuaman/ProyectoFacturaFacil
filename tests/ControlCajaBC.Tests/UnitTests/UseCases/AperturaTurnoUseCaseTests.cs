using System;
using System.Threading.Tasks;
using NUnit.Framework; 
using ControlCajaBC.Adapters.Output.Persistence.InMemory;
using ControlCajaBC.Application.UseCases;
using ControlCajaBC.Domain.ValueObjects;
using ControlCajaBC.Domain.Entities;

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
            var useCase = new AperturaTurnoUseCase(repo, uow);

            var codigo = CodigoCaja.New();
            var fecha  = new FechaHora(DateTime.UtcNow);
            var resp = new ResponsableCaja(Guid.NewGuid(), "Juan");
            var saldo  = new Monto(100m);

            // Act
            await useCase.HandleAsync(codigo, fecha, resp, saldo);

            // Assert: que el turno quedó almacenado
            var guardado = await repo.GetTurnoAbiertoAsync(codigo);
            Assert.That(guardado,           Is.Not.Null);
            Assert.That(guardado.CodigoCaja, Is.EqualTo(codigo));
            Assert.That(uow.WasCommitted,    Is.True);
        }
    }
}

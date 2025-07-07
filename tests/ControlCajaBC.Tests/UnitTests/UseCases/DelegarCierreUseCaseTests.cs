using System;
using System.Threading.Tasks;
using NUnit.Framework;
using ControlCajaBC.Adapters.Output.Persistence.InMemory;
using ControlCajaBC.Application.UseCases;
using ControlCajaBC.Domain.ValueObjects;
using ControlCajaBC.Domain.Entities;
using ControlCajaBC.Domain.Aggregates;  // 


namespace ControlCajaBC.Tests.UnitTests.UseCases
{
    public class DelegarCierreUseCaseTests
    {
        [Test]
        public async Task HandleAsync_CuandoHayTurnoAbierto_DeberiaDelegarYCometer()
        {
            // Arrange: pre-poblo un turno abierto
            var repo        = new InMemoryControlCajaRepository();
            var uow         = new InMemoryUnitOfWork();
            var codigo      = CodigoCaja.New();
            var apertura    = FechaHora.NowUtc();
            var responsable = new ResponsableCaja(Guid.NewGuid(), "Ana");
            var turno       = new TurnoCaja(codigo, apertura, responsable, new Monto(100m));
            await repo.AddTurnoCajaAsync(turno);

            var nuevoResponsable = new ResponsableCaja(Guid.NewGuid(), "Luis");
            var useCase = new DelegarCierreUseCase(repo, uow);

            // Act
            await useCase.HandleAsync(codigo, nuevoResponsable);

            // Assert: que el turno quedó con cierre delegado y UoW comprometió
            var almacenado = await repo.GetTurnoAbiertoAsync(codigo);
            Assert.That(almacenado, Is.Not.Null, "Debe seguir abierto tras delegar");
            Assert.That(almacenado!.ResponsableCierre, Is.EqualTo(nuevoResponsable),
                        "El cierre debe estar delegado al nuevo responsable.");
            Assert.That(uow.WasCommitted, Is.True,
                        "Debe haberse llamado a CommitAsync en la unidad de trabajo.");
        }

        [Test]
        public void HandleAsync_SinTurnoAbierto_DeberiaLanzar()
        {
            // Arrange: no hay turno abierto en repo
            var repo = new InMemoryControlCajaRepository();
            var uow  = new InMemoryUnitOfWork();
            var useCase = new DelegarCierreUseCase(repo, uow);
            var codigo  = CodigoCaja.New();
            var nuevoResponsable = new ResponsableCaja(Guid.NewGuid(), "Luis");

            // Act & Assert: lanza InvalidOperationException
            Assert.That(
                async () => await useCase.HandleAsync(codigo, nuevoResponsable),
                Throws.InvalidOperationException
                      .With.Message.Contain("No existe un turno abierto")
            );
        }
    }
}

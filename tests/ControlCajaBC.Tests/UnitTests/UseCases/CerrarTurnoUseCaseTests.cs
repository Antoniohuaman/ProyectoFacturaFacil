using System;
using System.Threading.Tasks;
using NUnit.Framework;
using ControlCajaBC.Adapters.Output.Persistence.InMemory;
using ControlCajaBC.Application.UseCases;
using ControlCajaBC.Domain.ValueObjects;
using ControlCajaBC.Domain.Entities;
using ControlCajaBC.Domain.Aggregates;

namespace ControlCajaBC.Tests.UnitTests.UseCases
{
    public class CerrarTurnoUseCaseTests
    {
        [Test]
        public async Task HandleAsync_CuandoHayTurnoAbierto_DeberiaCerrarYCometer()
        {
            // Arrange: pre-pobla un turno abierto
            var repo = new InMemoryControlCajaRepository();
            var uow  = new InMemoryUnitOfWork();
           var codigo = CodigoCaja.New();
            var apertura = new FechaHora(System.DateTime.UtcNow);
            var responsable = new ResponsableCaja(System.Guid.NewGuid(), "Ana");
            var saldoInicial = new Monto(50m);

            // Guarda el turno “abierto” manualmente
            var turno = new TurnoCaja(codigo, apertura, responsable, saldoInicial);
            await repo.AddTurnoCajaAsync(turno);

            var fechaCierre = new FechaHora(System.DateTime.UtcNow);
            var respCierre  = new ResponsableCaja(Guid.NewGuid(), "Luis");
            var useCase = new CerrarTurnoUseCase(repo, uow);

            // Act
            await useCase.HandleAsync(codigo, fechaCierre, respCierre);

            // Assert
            var cargado = await repo.GetTurnoAbiertoAsync(codigo);
            Assert.That(cargado,         Is.Null,  "El turno ya no debería estar abierto");
            Assert.That(uow.WasCommitted, Is.True, "Debe haberse llamado a CommitAsync");
        }
    }
}

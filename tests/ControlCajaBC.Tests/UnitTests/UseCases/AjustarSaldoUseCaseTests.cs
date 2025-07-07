using System;
using System.Threading.Tasks;
using NUnit.Framework;
using ControlCajaBC.Adapters.Output.Persistence.InMemory;
using ControlCajaBC.Application.UseCases;
using ControlCajaBC.Domain.Aggregates;
using ControlCajaBC.Domain.Entities;
using ControlCajaBC.Domain.ValueObjects;

namespace ControlCajaBC.Tests.UnitTests.UseCases
{
    public class AjustarSaldoUseCaseTests
    {
        [Test]
        public async Task HandleAsync_CuandoHayTurnoAbierto_DeberiaAjustarSaldoYCometer()
        {
            // Arrange: pre-poblo un turno abierto
            var repo         = new InMemoryControlCajaRepository();
            var uow          = new InMemoryUnitOfWork();
            var codigo       = CodigoCaja.New();
            var apertura     = new FechaHora(DateTime.UtcNow);
            var responsable  = new ResponsableCaja(Guid.NewGuid(), "Juan");
            var saldoInicial = new Monto(100m);
            // Guardo el turno abierto
            var turno = new TurnoCaja(codigo, apertura, responsable, saldoInicial);
            await repo.AddTurnoCajaAsync(turno);

            var useCase   = new AjustarSaldoUseCase(repo, uow);
            var nuevoSaldo = new Monto(180m);

            // Act
            await useCase.HandleAsync(codigo, nuevoSaldo);

            // Assert: el turno almacenado refleja el nuevo SaldoInicial
            var almacenado = await repo.GetTurnoAbiertoAsync(codigo);
            Assert.That(almacenado, Is.Not.Null, "El turno debería seguir abierto.");
            Assert.That(almacenado!.SaldoInicial.Value,
                        Is.EqualTo(180m),
                        "El SaldoInicial debe actualizarse al valor ajustado.");

            // Y debe haberse llamado a CommitAsync()
            Assert.That(uow.WasCommitted,
                        Is.True,
                        "Debe haberse llamado a CommitAsync en la unidad de trabajo.");
        }
    }
}

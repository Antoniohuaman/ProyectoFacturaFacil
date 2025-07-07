using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ControlCajaBC.Adapters.Output.Persistence.InMemory;
using ControlCajaBC.Application.UseCases;
using ControlCajaBC.Domain.Aggregates;
using ControlCajaBC.Domain.Entities;
using ControlCajaBC.Domain.ValueObjects;

namespace ControlCajaBC.Tests.UnitTests.UseCases
{
    public class RegistrarMovimientoUseCaseTests
    {
        [Test]
        public async Task HandleAsync_CuandoTurnoAbierto_DeberiaAgregarMovimientoYCometer()
        {
            // Arrange: pre-poblo un turno abierto
            var repo       = new InMemoryControlCajaRepository();
            var uow        = new InMemoryUnitOfWork();
            var codigo     = CodigoCaja.New();
            var apertura   = new FechaHora(DateTime.UtcNow);
            var responsable= new ResponsableCaja(Guid.NewGuid(), "Ana");
            var saldo0     = new Monto(100m);

            var turno = new TurnoCaja(codigo, apertura, responsable, saldo0);
            await repo.AddTurnoCajaAsync(turno);

            var useCase    = new RegistrarMovimientoUseCase(repo, uow);
            var fechaMov   = new FechaHora(DateTime.UtcNow);
            var monto      = new Monto(50m);
            var tipo       = TipoMovimiento.Ingreso;

            // Act
            var movId = await useCase.HandleAsync(codigo, fechaMov, monto, tipo);

            // Assert: el movimiento quedó en el agregado
            var turnoAct = await repo.GetTurnoAbiertoAsync(codigo);
            Assert.That(turnoAct, Is.Not.Null, "El turno debe seguir abierto.");
            
            var movs = turnoAct!.Movimientos;
            Assert.That(movs, Is.Not.Empty, "Debe haber al menos 1 movimiento.");

            var mov = movs.Single(m => m.Id == movId);
            Assert.That(mov, Is.Not.Null);
            Assert.That(mov.Monto, Is.EqualTo(monto));
            Assert.That(mov.Tipo, Is.EqualTo(tipo));

            // Y que UoW hizo Commit
            Assert.That(uow.WasCommitted, Is.True);
        }
    }
}

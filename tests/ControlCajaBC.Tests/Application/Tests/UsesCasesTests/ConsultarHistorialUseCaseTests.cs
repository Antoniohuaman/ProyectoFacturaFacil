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
    public class ConsultarHistorialUseCaseTests
    {
        [Test]
        public async Task HandleAsync_ConMovimientos_DeberiaRetornarLista()
        {
            // Arrange
            var repo        = new InMemoryControlCajaRepository();
            var codigo      = CodigoCaja.New();
            var apertura    = FechaHora.NowUtc();
            var responsable = new ResponsableCaja(Guid.NewGuid(), "Ana");
            var turno       = new TurnoCaja(codigo, apertura, responsable, new Monto(100m));

            // Registro dos movimientos
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            turno.RegistrarMovimiento(new MovimientoCaja(
                id1, codigo, FechaHora.NowUtc(), new Monto(40m), TipoMovimiento.Ingreso));
            turno.RegistrarMovimiento(new MovimientoCaja(
                id2, codigo, FechaHora.NowUtc(), new Monto(15m), TipoMovimiento.Egreso));

            await repo.AddTurnoCajaAsync(turno);
            var useCase = new ConsultarHistorialUseCase(repo);

            // Act
            var dtos = await useCase.HandleAsync(codigo);

            // Assert: vienen exactamente dos
            Assert.That(dtos,        Is.Not.Empty,    "Debe haber al menos 1 movimiento.");
            Assert.That(dtos.Count,  Is.EqualTo(2),   "Debe retornar exactamente 2.");

            // El primero
            var m1 = dtos.Single(m => m.Id == id1);
            Assert.That(m1,        Is.Not.Null);
            Assert.That(m1.Monto,  Is.EqualTo(40m));
            Assert.That(m1.Tipo,   Is.EqualTo(TipoMovimiento.Ingreso));

            // El segundo
            var m2 = dtos.Single(m => m.Id == id2);
            Assert.That(m2.Monto,  Is.EqualTo(15m));
            Assert.That(m2.Tipo,   Is.EqualTo(TipoMovimiento.Egreso));
        }

        [Test]
        public void HandleAsync_SinTurnoAbierto_DeberiaLanzar()
        {
            // Arrange
            var repo    = new InMemoryControlCajaRepository();
            var useCase = new ConsultarHistorialUseCase(repo);
            var codigo  = CodigoCaja.New();

            // Act & Assert: no hay turno abierto
            Assert.That(
                async () => await useCase.HandleAsync(codigo),
                Throws.InvalidOperationException
                      .With.Message.Contain("No existe un turno abierto")
            );
        }
    }
}

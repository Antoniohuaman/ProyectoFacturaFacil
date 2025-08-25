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
    public class AnularMovimientoUseCaseTests
    {
        [Test]
        public async Task HandleAsync_CuandoMovimientoNoExiste_DeberíaLanzar()
        {
            // Arrange
            var repo = new InMemoryControlCajaRepository();
            var uow  = new InMemoryUnitOfWork();
            var codigo = CodigoCaja.New();
            var apertura = new FechaHora(DateTime.UtcNow);
            var respApertura = new ResponsableCaja(Guid.NewGuid(), "Ana");
            var saldoInicial = new Monto(0m);

            // Creo y agrego un turno vacío
            var turno = new TurnoCaja(codigo, apertura, respApertura, saldoInicial);
            await repo.AddTurnoCajaAsync(turno);

            var useCase = new AnularMovimientoUseCase(repo, uow);
            var fakeId  = Guid.NewGuid();

            // Act & Assert: debe lanzar InvalidOperationException y contener el mensaje
            Assert.That(
                async () => await useCase.HandleAsync(codigo, fakeId),
                Throws.InvalidOperationException
                      .With.Message.Contain("Movimiento no encontrado")
            );
        }

        [Test]
        public async Task HandleAsync_CuandoMovimientoExiste_DeberíaEliminarloYCometer()
        {
            // Arrange
            var repo = new InMemoryControlCajaRepository();
            var uow  = new InMemoryUnitOfWork();
            var codigo = CodigoCaja.New();
            var apertura = new FechaHora(DateTime.UtcNow);
            var respApertura = new ResponsableCaja(Guid.NewGuid(), "Ana");
            var saldoInicial = new Monto(0m);

            // Creo y agrego un turno
            var turno = new TurnoCaja(codigo, apertura, respApertura, saldoInicial);

            // Inserto un movimiento
            var movId     = Guid.NewGuid();
            var fechaMov  = new FechaHora(DateTime.UtcNow);
            var monto     = new Monto(123.45m);
            var tipoMov   = TipoMovimiento.Egreso;
            var movimiento = new MovimientoCaja(movId, codigo, fechaMov, monto, tipoMov);

            turno.RegistrarMovimiento(movimiento);
            await repo.AddTurnoCajaAsync(turno);

            var useCase = new AnularMovimientoUseCase(repo, uow);

            // Act
            await useCase.HandleAsync(codigo, movId);

            // Assert: ya no aparece en la lista de movimientos
            var cargado = await repo.GetTurnoAbiertoAsync(codigo);
            Assert.That(cargado, Is.Not.Null, "El turno debería seguir abierto.");

            var movimientos = cargado!.Movimientos;
            Assert.That(movimientos, Is.Empty, "No debería quedar ningún movimiento.");

            // Y UoW debe haber hecho Commit
            Assert.That(uow.WasCommitted, Is.True, "Debe haberse llamado a CommitAsync.");
        }
    }
}

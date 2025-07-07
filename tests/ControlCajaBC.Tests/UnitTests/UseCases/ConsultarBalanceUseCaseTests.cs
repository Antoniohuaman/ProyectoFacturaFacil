// tests/ControlCajaBC.Tests/UnitTests/UseCases/ConsultarBalanceUseCaseTests.cs

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
    public class ConsultarBalanceUseCaseTests
    {
        [Test]
        public async Task HandleAsync_ConMovimientos_DeberiaRetornarSaldoYDiferencia()
        {
            // Arrange
            var repo        = new InMemoryControlCajaRepository();
            var codigo      = CodigoCaja.New();
            var apertura    = FechaHora.NowUtc();
            var responsable = new ResponsableCaja(Guid.NewGuid(), "Ana");
            var turno       = new TurnoCaja(codigo, apertura, responsable, new Monto(100m));

            // Registro movimientos +40, -15
            turno.RegistrarMovimiento(new MovimientoCaja(
                Guid.NewGuid(), codigo, FechaHora.NowUtc(), new Monto(40m), TipoMovimiento.Ingreso));
            turno.RegistrarMovimiento(new MovimientoCaja(
                Guid.NewGuid(), codigo, FechaHora.NowUtc(), new Monto(15m), TipoMovimiento.Egreso));

            await repo.AddTurnoCajaAsync(turno);

            var useCase = new ConsultarBalanceUseCase(repo);

            // Act
            var dto = await useCase.HandleAsync(codigo);

            // Assert
            Assert.That(dto.SaldoActual,  Is.EqualTo(125m), "SaldoActual = 100 + 40 - 15");
            Assert.That(dto.Diferencia,   Is.EqualTo(25m),  "Diferencia = 125 - 100");
        }

        [Test]
        public void HandleAsync_SinTurnoAbierto_DeberiaLanzar()
        {
            // Arrange
            var repo    = new InMemoryControlCajaRepository();
            var useCase = new ConsultarBalanceUseCase(repo);
            var codigo  = CodigoCaja.New();

            // Act & Assert
            Assert.That(
                async () => await useCase.HandleAsync(codigo),
                Throws.InvalidOperationException
                      .With.Message.Contain("No existe un turno abierto")
            );
        }
    }
}

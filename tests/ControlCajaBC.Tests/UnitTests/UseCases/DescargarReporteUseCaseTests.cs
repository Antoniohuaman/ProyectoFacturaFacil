// tests/ControlCajaBC.Tests/UnitTests/UseCases/DescargarReporteUseCaseTests.cs

using System;
using System.Threading.Tasks;
using NUnit.Framework;

// UseCases y puertos
using ControlCajaBC.Application.UseCases;
using ControlCajaBC.Application.Interfaces;

// Repositorio en memoria
using ControlCajaBC.Adapters.Output.Persistence.InMemory;

// Agregado raíz y entidades
using ControlCajaBC.Domain.Aggregates;
using ControlCajaBC.Domain.Entities;
using ControlCajaBC.Domain.ValueObjects;

namespace ControlCajaBC.Tests.UnitTests.UseCases
{
    public class DescargarReporteUseCaseTests
    {
        // Stub de IReportGenerator que devuelve siempre los mismos bytes
        private class FakePdfGen : IReportGenerator
        {
            public byte[] GenerateClosingReport(TurnoCaja turno)
                => new byte[] { 0x25, 0x50, 0x44, 0x46 }; // "%PDF"
        }

        [Test]
        public void HandleAsync_CuandoNoHayTurnoCerrado_DeberiaLanzar()
        {
            // Arrange
            var repo = new InMemoryControlCajaRepository();
            var pdfGen = new FakePdfGen();
            var useCase = new DescargarReporteUseCase(repo, pdfGen);
            var codigo = CodigoCaja.New();

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await useCase.HandleAsync(codigo);
            });

            Assert.That(ex.Message,
            Does.Contain("No existe un turno cerrado"));
        }

        [Test]
        public async Task HandleAsync_CuandoTurnoCerrado_DeberiaDevolverPdf()
        {
            // Arrange: preparamos un turno YA cerrado en el repo
            var repo         = new InMemoryControlCajaRepository();
            var pdfGen       = new FakePdfGen();
            var codigo       = CodigoCaja.New();
            var apertura     = FechaHora.NowUtc();
            var aperturaResp = new ResponsableCaja(Guid.NewGuid(), "Ana");

            // Creamos el turno y le añadimos un movimiento
            var turno = new TurnoCaja(codigo, apertura, aperturaResp, new Monto(100m));
            turno.RegistrarMovimiento(new MovimientoCaja(
                Guid.NewGuid(),
                codigo,
                FechaHora.NowUtc(),
                new Monto(20m),
                TipoMovimiento.Ingreso
            ));

            // Cerramos el turno
            turno.CerrarTurno(
                FechaHora.NowUtc(),
                new ResponsableCaja(Guid.NewGuid(), "Luis")
            );

            await repo.AddTurnoCajaAsync(turno);
            var useCase = new DescargarReporteUseCase(repo, pdfGen);

            // Act
            var dto = await useCase.HandleAsync(codigo);

            // Assert: contenido y nombre de archivo
            Assert.That(dto.ContenidoPdf,
                        Is.EqualTo(new byte[] { 0x25, 0x50, 0x44, 0x46 }),
                        "Debe devolver los bytes del PDF");
            Assert.That(dto.NombreArchivo,
                        Does.StartWith($"ReporteCierre_{codigo.Value}_"),
                        "El nombre debe incluir el código y timestamp");
        }
    }
}

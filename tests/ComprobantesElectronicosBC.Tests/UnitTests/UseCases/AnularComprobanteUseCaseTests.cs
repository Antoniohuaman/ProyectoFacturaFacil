using System;
using System.Threading.Tasks;
using NUnit.Framework;

using ComprobantesElectronicosBC.Application.DTOs;
using ComprobantesElectronicosBC.Application.UseCases;
using ComprobantesElectronicosBC.Adapters.Output.Persistence.InMemory;

using ComprobantesElectronicosBC.Domain.Aggregates;
using ComprobantesElectronicosBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.UnitTests.UseCases
{
    [TestFixture]
    public class AnularComprobanteUseCaseTests
    {
        // ----------------- Helpers: construimos datos coherentes -----------------

        private static ComprobanteElectronico CrearBorradorBase()
        {
            var tipo    = TipoDeComprobante.Create("01"); // Factura
            var moneda  = Moneda.Create("PEN");
            var ahora   = DateTime.UtcNow;
            var emision = FechaEmision.Create(DateOnly.FromDateTime(DateTime.Now), tipo.Codigo, ahora);

            var forma   = FormaDePago.Contado();
            var venc    = FechaVencimiento.ParaFormaDePago(forma, emision.Fecha);

            var dirEmi  = DireccionPostal.Create("150101", "Av. Principal 123", "Lima", "Lima", "Lima");
            var emisor  = EmisorSnapshot.Create("20123456789", "MI EMPRESA SAC", dirEmi);

            var docCli  = DocumentoIdentidad.Create("6", "20100070970");
            var dirCli  = DireccionPostal.FromCliente(docCli, "150101", "Calle 1", "Lima", "Lima", "Lima");
            var cliente = ClienteSnapshot.Create(docCli, "CLIENTE SAC", dirCli);

            var agg = ComprobanteElectronico.CrearBorrador(
                tipo, emisor, cliente, moneda, emision, forma, venc, ahora);

            // una línea mínima para que tenga montos
            var desc   = DescripcionProducto.Create("Servicio", null);
            var um     = UnidadDeMedida.NIU;
            var qty    = Cantidad.Create(1m);
            var precio = ImporteMonetario.Create(100m, moneda);
            var igv    = ImpuestoIGV.Gravado18();

            agg.AgregarLinea(desc, um, qty, precio, igv, precioIncluyeIgv: false, DescuentoLinea.None);
            return agg;
        }

        /// <summary>Simula que el documento ya pasó por el servicio externo y quedó Aceptado.</summary>
        private static ComprobanteElectronico CrearComprobanteAceptado()
        {
            var agg = CrearBorradorBase();

            // En un flujo real esto ocurre al “Emitir” + respuesta CDR.
            // Aquí solo lo simulamos para probar la anulación post-aceptación.
            agg.AsignarSerieYNumero("F001", 1);
            agg.Emitir();
            agg.MarcarAceptado();

            Assert.That(agg.EstadoCodigo, Is.EqualTo("ACCEPTED")); // sanity
            return agg;
        }

        // ----------------- Tests -----------------

        [Test]
        public async Task Anular_Comprobante_Aceptado_MarcaCancelled()
        {
            // Arrange
            var repo = new InMemoryComprobanteRepository();
            var uow  = new InMemoryUnitOfWork();
            var uc   = new AnularComprobanteUseCase(repo, uow);

            var aceptado = CrearComprobanteAceptado();
            await repo.AddAsync(aceptado);

            var fechaBaja = DateOnly.FromDateTime(DateTime.Now);
            var input = new AnularComprobanteInput(
                ComprobanteId: aceptado.ComprobanteId,
                FechaBaja: fechaBaja,
                Motivo: "Cliente solicitó anulación"
            );

            // Act
            var output = await uc.Handle(input);

            // Assert
            Assert.That(output.ComprobanteId, Is.EqualTo(aceptado.ComprobanteId));
            Assert.That(output.Estado, Is.EqualTo("CANCELLED"));
            Assert.That(output.FechaBaja, Is.EqualTo(fechaBaja));
        }

        [Test]
        public void Anular_Comprobante_EnDraft_DebeFallar()
        {
            // Arrange
            var repo = new InMemoryComprobanteRepository();
            var uow  = new InMemoryUnitOfWork();
            var uc   = new AnularComprobanteUseCase(repo, uow);

            var borrador = CrearBorradorBase();
            Assert.That(borrador.EstadoCodigo, Is.EqualTo("DRAFT"));

            // Guardamos el borrador
            Assert.DoesNotThrowAsync(() => repo.AddAsync(borrador));

            var input = new AnularComprobanteInput(
                ComprobanteId: borrador.ComprobanteId,
                FechaBaja: DateOnly.FromDateTime(DateTime.Now),
                Motivo: "No corresponde anular un borrador"
            );

            // Act + Assert (el agregado debe impedir esta transición)
            Assert.ThrowsAsync<InvalidOperationException>(async () => await uc.Handle(input));
        }

        // Si decides permitir anular desde ENVIADO (PendingValidation),
        // puedes agregar luego un helper que lo deje en ese estado
        // y aquí afirmar CANCELLED. Si tu agregado lo prohíbe, debería lanzar.
    }
}

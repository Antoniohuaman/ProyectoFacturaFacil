using System;
using System.Threading.Tasks;
using NUnit.Framework;
using ComprobantesElectronicosBC.Domain.Aggregates;
using ComprobantesElectronicosBC.Domain.Repositories;
using ComprobantesElectronicosBC.Adapters.Output.Persistence.InMemory;
using SharedKernel.ValueObjects;
using SharedKernel.Exceptions;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.Domain.Concurrency
{
    [TestFixture]
    public class ComprobanteConcurrencyTests
    {
        private IComprobanteRepository _repo = null!;

        [SetUp]
        public void SetUp()
        {
            _repo = new InMemoryComprobanteRepository();
        }

        private static ComprobanteElectronico NuevoBorrador()
        {
            var tipo = TipoDeComprobante.Create("FACTURA");
            var emisor = EmisorSnapshot.Create(EmpresaId.From("20600893409"), TenantId.New(), EstablecimientoId.From(Guid.NewGuid()), "20600893409", "ACME", DomicilioFiscal.From("PE","Av. 1"));
            var cliente = ClienteSnapshot.Create(EmpresaId.From("20600893409"), TenantId.New(), DocumentoIdentidad.Crear(TipoDocumento.Ruc, "20600893409"), "ACME", DomicilioFiscal.From("PE","Av. 1"), null);
            var moneda = Moneda.Create("PEN");
            var fecha = FechaEmision.Create(DateOnly.FromDateTime(DateTime.Now), tipo.Codigo, DateTime.UtcNow);
            var forma = FormaDePago.Contado();
            var venc = FechaVencimiento.ParaFormaDePago(forma, fecha.Fecha, null);
            var usuario = new UsuarioSnapshot("u","n","c");
            return ComprobanteElectronico.CrearBorrador(tipo, emisor, cliente, moneda, fecha, forma, venc, usuario, DateTimeOffset.UtcNow);
        }

        [Test]
        public void Update_con_version_obsoleta_lanza_ConcurrencyException()
        {
            var agg = NuevoBorrador();
            _repo.AddAsync(agg).GetAwaiter().GetResult();

            // Transiciones de negocio previas al persistir: incrementan la versión en memoria.
            var monedaLocal = Moneda.Create("PEN");
            agg.AgregarLinea(
                DescripcionProducto.Create("Item"),
                SharedKernel.ValueObjects.UnidadDeMedida.From("NIU"),
                Cantidad.Create(1m),
                ImporteMonetario.Create(100m, monedaLocal),
                SharedKernel.ValueObjects.AfectacionImpuesto.From("10"),
                SharedKernel.ValueObjects.TasaImpuesto.FromPercent(18m).CompatibilizarCon(SharedKernel.ValueObjects.AfectacionImpuesto.From("10")),
                precioIncluyeIgv: false);
            agg.AsignarSerieYNumero("F001", 1);
            agg.Emitir(); // Version pasa de 0 -> 1

            // Persistir con expectedVersion antiguo (0) debe provocar colisión de concurrencia.
            Assert.Throws<ConcurrencyException>(() =>
                _repo.UpdateAsync(agg, expectedVersion: 0).GetAwaiter().GetResult());
        }
    }
}

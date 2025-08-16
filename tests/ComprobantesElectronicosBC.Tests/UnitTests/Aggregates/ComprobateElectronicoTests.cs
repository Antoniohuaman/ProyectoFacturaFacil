using System;
using NUnit.Framework;
using ComprobantesElectronicosBC.Domain.Aggregates;
using ComprobantesElectronicosBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace ComprobantesElectronicosBC.Tests
{
    [TestFixture]
    public class ComprobanteElectronicoTests
    {
        // Fecha/hora fija para pruebas determinísticas
        private static readonly DateTime Now = new(2025, 8, 10, 12, 0, 0);

        // ========= Helpers de armado de escenario =========

        private static EmisorSnapshot EmisorDefault()
        {
            var dir = DireccionPostal.Create("150101", "Av. Principal 123", "Lima", "Lima", "Lima");
            return EmisorSnapshot.Create("20123456789", "Mi Empresa SAC", dir);
        }

        private static ClienteSnapshot ClienteBoletaDni()
        {
            var doc = DocumentoIdentidad.CreateDni("12345678");
            var dir = DireccionPostal.FromCliente(doc, "150101", "Calle Cliente 456", "Lima", "Lima", "Lima");
            return ClienteSnapshot.Create(doc, "Juan Perez", dir);
        }

        private static (ComprobanteElectronico agg, DateOnly emision, Moneda moneda) CrearBorradorBoletaContado()
        {
            var tipo     = TipoDeComprobante.Boleta; // evita requisito de RUC en cliente
            var emisor   = EmisorDefault();
            var cliente  = ClienteBoletaDni();
            var moneda   = Moneda.PEN();
            var emision  = FechaEmision.Create(DateOnly.FromDateTime(Now), tipo.Codigo, Now);
            var forma    = FormaDePago.ContadoEfectivo();
            var vence    = FechaVencimiento.ParaFormaDePago(forma, emision.Fecha);

            var agg = ComprobanteElectronico.CrearBorrador(
                tipo, emisor, cliente, moneda, emision, forma, vence, Now);

            return (agg, emision.Fecha, moneda);
        }

        private static Guid AgregarLineaDefault(ComprobanteElectronico agg, Moneda moneda,
                                                decimal precioUnitario = 100m, decimal cantidad = 2m,
                                                bool precioIncluyeIgv = false, DescuentoLinea? desc = null)
        {
            var descripcion  = DescripcionProducto.Create("Producto A", "Detalle de prueba");
            var unidad       = UnidadDeMedida.NIU();
            var qty          = Cantidad.Create(cantidad);
            var precio       = ImporteMonetario.Create(precioUnitario, moneda);
            var igv          = ImpuestoIGV.Gravado18(); // 18%
            return agg.AgregarLinea(descripcion, unidad, qty, precio, igv, precioIncluyeIgv, desc);
        }

        // ========= Pruebas =========

        [Test]
        public void CrearBorrador_Contado_VencimientoIgualAEmision()
        {
            var (agg, emisionFecha, _) = CrearBorradorBoletaContado();

            Assert.That(agg.Estado, Is.EqualTo(EstadoComprobante.Borrador));
            Assert.That(agg.Vencimiento.Value, Is.EqualTo(emisionFecha));
            Assert.That(agg.Lineas.Count, Is.EqualTo(0));
            Assert.That(agg.Total, Is.EqualTo(0m));
            Assert.That(agg.EstadoCodigo, Is.EqualTo("DRAFT"));
        }

        [Test]
        public void AgregarLinea_CalculaTotalesConDescuentoDeLinea()
        {
            var (agg, _, moneda) = CrearBorradorBoletaContado();

            // Línea: P=100, Q=2, IGV 18%, precio SIN IGV, DESC LINEA 10%
            AgregarLineaDefault(agg, moneda, 100m, 2m, false, DescuentoLinea.FromPorcentaje(10m));

            // Sin descuento global:
            // Base antes desc: 100*2 = 200
            // Descuento línea 10%: 20  → Base después: 180
            // IGV 18% de 180 = 32.40
            // Total = 212.40
            Assert.That(agg.SubtotalBase, Is.EqualTo(180m));
            Assert.That(agg.IgvTotal, Is.EqualTo(32.40m));
            Assert.That(agg.Total, Is.EqualTo(212.40m));
        }

        [Test]
        public void DescuentoGlobal_Porcentaje_RecalculaBaseEIgvConProrrateo()
        {
            var (agg, _, moneda) = CrearBorradorBoletaContado();
            AgregarLineaDefault(agg, moneda, 100m, 2m, false, DescuentoLinea.FromPorcentaje(10m));

            // Aplicar descuento global 5% sobre la BASE (180 -> 171)
            // IGV se recalcula por prorrateo: 171 * 18% = 30.78
            agg.CambiarDescuentoGlobal(DescuentoGlobal.FromPorcentaje(5m));

            Assert.That(agg.SubtotalBase, Is.EqualTo(180m));
            Assert.That(agg.DescuentoGlobalMonto, Is.EqualTo(9.00m));
            Assert.That(agg.IgvTotal, Is.EqualTo(30.78m));
            Assert.That(agg.Total, Is.EqualTo(201.78m));
        }

        [Test]
        public void AsignarSerieYNumero_CompatibleConBoleta_Ok()
        {
            var (agg, _, moneda) = CrearBorradorBoletaContado();
            AgregarLineaDefault(agg, moneda);

            // Serie B* compatible con Boleta ("03")
            agg.AsignarSerieYNumero("B001", 1);
            Assert.That(agg.SerieNumero!.Serie, Is.EqualTo("B001"));
            Assert.That(agg.SerieNumero.Numero, Is.EqualTo(1));
        }

        [Test]
        public void AsignarSerieYNumero_IncompatibleConBoleta_Lanza()
        {
            var (agg, _, moneda) = CrearBorradorBoletaContado();
            AgregarLineaDefault(agg, moneda);

            // Serie F* no compatible con Boleta
            var ex = Assert.Throws<InvalidOperationException>(() => agg.AsignarSerieYNumero("F001", 1));
            Assert.That(ex!.Message, Does.Contain("corresponde a Factura"));
        }

        [Test]
        public void Emitir_Aceptar_FlujoFeliz_CambiaEstadosYFechas()
        {
            var (agg, _, moneda) = CrearBorradorBoletaContado();
            AgregarLineaDefault(agg, moneda);
            agg.AsignarSerieYNumero("B001", 1);

            agg.Emitir();
            Assert.That(agg.Estado, Is.EqualTo(EstadoComprobante.Enviado));
            Assert.That(agg.EstadoCodigo, Is.EqualTo("SENT"));
            Assert.That(agg.EnviadoEnUtc, Is.Not.Null);

            agg.MarcarAceptado();
            Assert.That(agg.Estado, Is.EqualTo(EstadoComprobante.Aceptado));
            Assert.That(agg.EstadoCodigo, Is.EqualTo("ACCEPTED"));
            Assert.That(agg.AceptadoEnUtc, Is.Not.Null);
        }

        [Test]
        public void MarcarAceptado_DesdeBorrador_Lanza()
        {
            var (agg, _, _) = CrearBorradorBoletaContado();

            var ex = Assert.Throws<InvalidOperationException>(() => agg.MarcarAceptado());
            Assert.That(ex!.Message, Does.Contain("ENVIADO"));
        }

        [Test]
        public void Emitir_Corregir_Reemitir_PermiteEditarEnCorregir()
        {
            var (agg, _, moneda) = CrearBorradorBoletaContado();
            var idLinea = AgregarLineaDefault(agg, moneda, 50m, 1m, false, DescuentoLinea.None);
            agg.AsignarSerieYNumero("B001", 99);

            agg.Emitir();
            Assert.That(agg.Estado, Is.EqualTo(EstadoComprobante.Enviado));

            agg.MarcarCorregir("Error de validación X");
            Assert.That(agg.Estado, Is.EqualTo(EstadoComprobante.Corregir));
            Assert.That(agg.UltimoErrorTecnico, Is.EqualTo("Error de validación X"));

            // En "Corregir" se puede editar
            agg.EditarLinea(idLinea, cantidad: Cantidad.Create(2m));
            Assert.That(agg.SubtotalBase, Is.EqualTo(100m)); // 50*2 base (sin IGV, sin desc)

            // Reemitir
            agg.Emitir();
            Assert.That(agg.Estado, Is.EqualTo(EstadoComprobante.Enviado));
        }

        [Test]
        public void Enviado_Rechazado_Anulado_FlujoEstados()
        {
            var (agg, _, moneda) = CrearBorradorBoletaContado();
            AgregarLineaDefault(agg, moneda);
            agg.AsignarSerieYNumero("B001", 5);
            agg.Emitir();

            // Rechazado desde Enviado
            agg.MarcarRechazado("2001", "Error en estructura XML");
            Assert.That(agg.Estado, Is.EqualTo(EstadoComprobante.Rechazado));
            Assert.That(agg.EstadoCodigo, Is.EqualTo("REJECTED"));
            Assert.That(agg.UltimoCdrCodigo, Is.EqualTo("2001"));

            // No se puede anular desde Rechazado (debe estar Aceptado)
            Assert.Throws<InvalidOperationException>(() => agg.MarcarAnulado(DateTimeOffset.UtcNow));

            // Otro flujo: Aceptar y luego Anular
            var (agg2, _, moneda2) = CrearBorradorBoletaContado();
            AgregarLineaDefault(agg2, moneda2);
            agg2.AsignarSerieYNumero("B001", 6);
            agg2.Emitir();
            agg2.MarcarAceptado();

            Assert.That(agg2.Estado, Is.EqualTo(EstadoComprobante.Aceptado));

            var tsBaja = new DateTimeOffset(Now).AddDays(1);
            agg2.MarcarAnulado(tsBaja);

            Assert.That(agg2.Estado, Is.EqualTo(EstadoComprobante.Anulado));
            Assert.That(agg2.EstadoCodigo, Is.EqualTo("CANCELLED"));
            Assert.That(agg2.AnuladoEnUtc, Is.EqualTo(tsBaja));
        }

        [Test]
        public void MonedaDeLinea_DistintaALaDelDocumento_Lanza()
        {
            var (agg, _, monedaDoc) = CrearBorradorBoletaContado();
            var descripcion  = DescripcionProducto.Create("Servicio X");
            var unidad       = UnidadDeMedida.E48();
            var qty          = Cantidad.Create(1m);
            var precioUsd    = ImporteMonetario.Create(10m, Moneda.USD());
            var igv          = ImpuestoIGV.Gravado18();

            Assert.That(monedaDoc.Codigo, Is.EqualTo("PEN"));

            var ex = Assert.Throws<InvalidOperationException>(() =>
                agg.AgregarLinea(descripcion, unidad, qty, precioUsd, igv, false));
            Assert.That(ex!.Message, Does.Contain("moneda de la línea"));
        }
    }
}

using NUnit.Framework;
using ComprobantesElectronicosBC.Domain.Entities;
using ComprobantesElectronicosBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.UnitTests.Entities
{
    [TestFixture]
    public class ComprobanteLineaTests
    {
        // ---------- Helpers ----------
        private static Moneda PEN => Moneda.PEN();
    // ...existing code...
    private static UnidadDeMedida NIU => UnidadDeMedida.NIU;
    private static UnidadDeMedida KGM => UnidadDeMedida.KGM;
        private static DescripcionProducto Desc(string n = "Producto X", string? d = null)
            => DescripcionProducto.Create(n, d);

        private static ComprobanteLinea NuevaLinea(
            decimal precio, bool incluyeIgv, decimal cantidad, ImpuestoIGV? igv = null,
            UnidadDeMedida? um = null, DescuentoLinea? desc = null)
        {
            var precioVo   = ImporteMonetario.Create(precio, PEN);
            var cantVo     = Cantidad.Create(cantidad);
            var umVo       = um ?? NIU;
            var impuesto   = igv ?? ImpuestoIGV.Gravado18();

            return ComprobanteLinea.Create(
                numeroLinea: 1,
                descripcion: Desc(),
                um: umVo,
                cantidad: cantVo,
                precioUnitario: precioVo,
                precioIncluyeIgv: incluyeIgv,
                impuesto: impuesto,
                descuento: desc,
                centroDeCosto: null
            );
        }

        // ---------- Casos ----------

        [Test]
        public void Create_Gravado18_PrecioSinIgv_Cant2_CalculaBien()
        {
            var linea = NuevaLinea(precio: 100m, incluyeIgv: false, cantidad: 2m, igv: ImpuestoIGV.Gravado18());

            Assert.Multiple(() =>
            {
                Assert.That(linea.UnitPriceSinIgv.Monto, Is.EqualTo(100m));
                Assert.That(linea.UnitPriceConIgv.Monto, Is.EqualTo(118m));
                Assert.That(linea.BaseAntesDescuento.Monto, Is.EqualTo(200m));
                Assert.That(linea.Igv.Monto, Is.EqualTo(36m));
                Assert.That(linea.ImporteTotal.Monto, Is.EqualTo(236m));
                Assert.That(linea.Moneda.Codigo, Is.EqualTo("PEN"));
            });
        }

        [Test]
        public void Create_Gravado18_PrecioConIgv_Cant2_CalculaBien()
        {
            var linea = NuevaLinea(precio: 118m, incluyeIgv: true, cantidad: 2m, igv: ImpuestoIGV.Gravado18());

            Assert.Multiple(() =>
            {
                Assert.That(linea.UnitPriceSinIgv.Monto, Is.EqualTo(100m));
                Assert.That(linea.UnitPriceConIgv.Monto, Is.EqualTo(118m));
                Assert.That(linea.BaseAntesDescuento.Monto, Is.EqualTo(200m));
                Assert.That(linea.Igv.Monto, Is.EqualTo(36m));
                Assert.That(linea.ImporteTotal.Monto, Is.EqualTo(236m));
            });
        }

        [Test]
        public void Descuento_Porcentaje10_SobreBase_CalculaBien()
        {
            var desc = DescuentoLinea.FromPorcentaje(10m); // 10%
            var linea = NuevaLinea(precio: 100m, incluyeIgv: false, cantidad: 2m, igv: ImpuestoIGV.Gravado18(), desc: desc);

            // Base 200 → descuento 20 → base 180 → IGV 32.40 → total 212.40
            Assert.Multiple(() =>
            {
                Assert.That(linea.BaseAntesDescuento.Monto, Is.EqualTo(200m));
                Assert.That(linea.DescuentoMonto.Monto,     Is.EqualTo(20m));
                Assert.That(linea.BaseImponible.Monto,      Is.EqualTo(180m));
                Assert.That(linea.Igv.Monto,                Is.EqualTo(32.40m));
                Assert.That(linea.ImporteTotal.Monto,       Is.EqualTo(212.40m));
            });
        }

        [Test]
        public void Descuento_MontoFijo15_CalculaBien()
        {
            var desc = DescuentoLinea.FromMonto(15m);
            var linea = NuevaLinea(precio: 100m, incluyeIgv: false, cantidad: 2m, igv: ImpuestoIGV.Gravado18(), desc: desc);

            // Base 200 → desc 15 → base 185 → IGV 33.30 → total 218.30
            Assert.Multiple(() =>
            {
                Assert.That(linea.BaseAntesDescuento.Monto, Is.EqualTo(200m));
                Assert.That(linea.DescuentoMonto.Monto,     Is.EqualTo(15m));
                Assert.That(linea.BaseImponible.Monto,      Is.EqualTo(185m));
                Assert.That(linea.Igv.Monto,                Is.EqualTo(33.30m));
                Assert.That(linea.ImporteTotal.Monto,       Is.EqualTo(218.30m));
            });
        }

        [Test]
        public void Exonerado_SinIgv_CalculaBien()
        {
            var linea = NuevaLinea(precio: 100m, incluyeIgv: false, cantidad: 2m, igv: ImpuestoIGV.Exonerado());

            Assert.Multiple(() =>
            {
                Assert.That(linea.UnitPriceSinIgv.Monto, Is.EqualTo(100m));
                Assert.That(linea.UnitPriceConIgv.Monto, Is.EqualTo(100m));
                Assert.That(linea.Igv.Monto,             Is.EqualTo(0m));
                Assert.That(linea.ImporteTotal.Monto,    Is.EqualTo(200m));
            });
        }

        [Test]
        public void Create_NIU_ConDecimales_LanzaPorPrecision()
        {
            // Cantidad 1.5 con NIU (escala 0) debe fallar en el constructor (enforce por UM)
            Assert.That(() =>
            {
                _ = NuevaLinea(precio: 100m, incluyeIgv: false, cantidad: 1.5m, igv: ImpuestoIGV.Gravado18(), um: NIU);
            }, Throws.Exception);
        }

        [Test]
        public void CambiarUnidad_AKGM_PermiteCantidadCon3Decimales()
        {
            // Crea con NIU y cantidad entera
            var linea = NuevaLinea(precio: 10m, incluyeIgv: false, cantidad: 1m, igv: ImpuestoIGV.Gravado18(), um: NIU);
            // Cambia a KGM y luego a cantidad con 3 decimales → OK
            Assert.That(() => linea.CambiarUnidad(KGM), Throws.Nothing);
            Assert.That(() => linea.CambiarCantidad(Cantidad.Create(1.234m)), Throws.Nothing);
        }

        [Test]
        public void CambiarPrecio_MonedaDiferente_Lanza()
        {
            var linea = NuevaLinea(precio: 10m, incluyeIgv: false, cantidad: 1m);

            var usd = Moneda.USD();
            var nuevoPrecioUsd = ImporteMonetario.Create(5m, usd);

            Assert.That(() => linea.CambiarPrecio(nuevoPrecioUsd), Throws.InvalidOperationException);
        }

        [Test]
        public void CambiarDescuento_A_None_ReseteaMontoDescuento()
        {
            var desc = DescuentoLinea.FromPorcentaje(5m);
            var linea = NuevaLinea(precio: 100m, incluyeIgv: false, cantidad: 1m, desc: desc);

            // Asegura que haya descuento
            Assert.That(linea.DescuentoMonto.Monto, Is.GreaterThan(0m));

            // Pasa a None
            linea.CambiarDescuento(DescuentoLinea.None);

            Assert.Multiple(() =>
            {
                Assert.That(linea.DescuentoMonto.Monto, Is.EqualTo(0m));
                // Base/IGV/Total iguales a “sin descuento”
                Assert.That(linea.BaseImponible.Monto, Is.EqualTo(100m));
                Assert.That(linea.Igv.Monto,           Is.EqualTo(18m));
                Assert.That(linea.ImporteTotal.Monto,  Is.EqualTo(118m));
            });
        }
    }
}

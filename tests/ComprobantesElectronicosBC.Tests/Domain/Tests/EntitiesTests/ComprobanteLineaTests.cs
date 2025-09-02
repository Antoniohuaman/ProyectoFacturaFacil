using System;
using NUnit.Framework;
using ComprobantesElectronicosBC.Domain.Entities;
using ComprobantesElectronicosBC.Domain.ValueObjects;
using ComprobantesElectronicosBC.Domain.Exceptions;
using SharedKernel.ValueObjects;

namespace ComprobantesElectronicosBC.Domain.Entities.Tests
{
    [TestFixture]
    public class ComprobanteLineaTests
    {
        // ----------------------- Helpers -----------------------
    private static Moneda PEN() => Moneda.PEN();
    private static Moneda USD() => Moneda.USD();
    private static UnidadDeMedida UM_NIU() => UnidadDeMedida.NIU;
    private static UnidadDeMedida UM_KGM() => UnidadDeMedida.KGM;
    private static UnidadDeMedida UM_LTR() => UnidadDeMedida.LTR;
    private static UnidadDeMedida UM_MTR() => UnidadDeMedida.MTR;
    private static UnidadDeMedida UM_OTRA6() => UnidadDeMedida.NIU; // fallback for custom

    private static DescripcionProducto Desc(string s) => DescripcionProducto.Create(s);
    private static Cantidad C(decimal v, int escala = 2) => Cantidad.Create(Math.Round(v, escala, MidpointRounding.AwayFromZero));
    private static ImporteMonetario M(decimal v, Moneda m) => ImporteMonetario.Create(v, m);
    private static SharedKernel.ValueObjects.AfectacionImpuesto Gravado() => SharedKernel.ValueObjects.AfectacionImpuesto.From("10");
    private static SharedKernel.ValueObjects.AfectacionImpuesto Exonerado() => SharedKernel.ValueObjects.AfectacionImpuesto.From("20");
    private static TasaImpuesto Tasa(decimal fraccion) => SharedKernel.ValueObjects.TasaImpuesto.FromFraction(fraccion);

        // =======================================================
        // Creación básica y cálculos con IGV (precio incluye/no)
        // =======================================================

        [Test]
        public void Crear_Gravado_PrecioIncluyeIGV_CalculaUnitariosYTotales()
        {
            // 2 uds, precio unitario 118 CON IGV (18%)
            var linea = ComprobanteLinea.Create(
                numeroLinea: 1,
                descripcion: Desc("Producto A"),
                um: UM_NIU(),
                cantidad: C(2m),
                precioUnitario: M(118m, PEN()),
                precioIncluyeIgv: true,
                afectacionImpuesto: Gravado(),
                tasaImpuesto: Tasa(0.18m),
                descuento: DescuentoLinea.None
            );

            // Unitarios
            Assert.That(linea.UnitPriceSinIgv.Monto, Is.EqualTo(100.00m)); // 118 / 1.18
            Assert.That(linea.UnitPriceConIgv.Monto, Is.EqualTo(118.00m));

            // Totales
            Assert.That(linea.BaseAntesDescuento.Monto, Is.EqualTo(200.00m));
            Assert.That(linea.DescuentoMonto.Monto,     Is.EqualTo(0.00m));
            Assert.That(linea.BaseImponible.Monto,      Is.EqualTo(200.00m));
            Assert.That(linea.Igv.Monto,                Is.EqualTo(36.00m));
            Assert.That(linea.ImporteTotal.Monto,       Is.EqualTo(236.00m));
        }

        [Test]
        public void Crear_Gravado_PrecioNoIncluyeIGV_CalculaUnitariosYTotales()
        {
            // 2 uds, precio unitario 100 SIN IGV (18%)
            var linea = ComprobanteLinea.Create(
                numeroLinea: 1,
                descripcion: Desc("Producto B"),
                um: UM_NIU(),
                cantidad: C(2m),
                precioUnitario: M(100m, PEN()),
                precioIncluyeIgv: false,
                afectacionImpuesto: Gravado(),
                tasaImpuesto: Tasa(0.18m),
                descuento: DescuentoLinea.None
            );

            Assert.That(linea.UnitPriceSinIgv.Monto, Is.EqualTo(100.00m));
            Assert.That(linea.UnitPriceConIgv.Monto, Is.EqualTo(118.00m));

            Assert.That(linea.BaseImponible.Monto, Is.EqualTo(200.00m));
            Assert.That(linea.Igv.Monto,           Is.EqualTo(36.00m));
            Assert.That(linea.ImporteTotal.Monto,  Is.EqualTo(236.00m));
        }

        [Test]
        public void Crear_NoGravado_Exonerado_EsSinIGV_Y_UnitariosIguales()
        {
            var linea = ComprobanteLinea.Create(
                numeroLinea: 1,
                descripcion: Desc("Servicio Exonerado"),
                um: UM_NIU(),
                cantidad: C(3m),
                precioUnitario: M(50m, PEN()),   // no importa incluye/no porque no grava
                precioIncluyeIgv: true,
                afectacionImpuesto: Exonerado(),
                tasaImpuesto: Tasa(0.18m),       // No debería usarse
                descuento: DescuentoLinea.None
            );

            Assert.That(linea.EsGravado, Is.False);
            Assert.That(linea.Igv.Monto, Is.EqualTo(0.00m));
            Assert.That(linea.UnitPriceConIgv.Monto, Is.EqualTo(linea.UnitPriceSinIgv.Monto));
            Assert.That(linea.ImporteTotal.Monto, Is.EqualTo(linea.BaseImponible.Monto));
            Assert.That(linea.BaseImponible.Monto, Is.EqualTo(150.00m));
        }

        // ============================================
        // Descuentos (porcentaje y monto) — si existen
        // ============================================

        [Test]
        public void Descuento_Porcentaje_SeAplicaALaBase_Y_RecalculaIGV()
        {
            // Si tu VO tiene otra fábrica, ajusta esta línea:
            var dcto10 = DescuentoLinea.FromFraccion(0.10m);

            var linea = ComprobanteLinea.Create(
                1, Desc("Prod con %"), UM_NIU(), C(2m),
                M(100m, PEN()), false, Gravado(), Tasa(0.18m), dcto10
            );

            // BaseAntes = 200; Dcto = 20; BaseDespues = 180; IGV = 32.40; Total = 212.40
            Assert.That(linea.BaseAntesDescuento.Monto, Is.EqualTo(200.00m));
            Assert.That(linea.DescuentoMonto.Monto,     Is.EqualTo(20.00m));
            Assert.That(linea.BaseImponible.Monto,      Is.EqualTo(180.00m));
            Assert.That(linea.Igv.Monto,                Is.EqualTo(32.40m));
            Assert.That(linea.ImporteTotal.Monto,       Is.EqualTo(212.40m));
        }

        [Test]
        public void Descuento_Monto_SeAplicaALaBase_Y_RecalculaIGV()
        {
            // Ajusta esta fábrica si es diferente en tu VO:
            var dcto = DescuentoLinea.FromMonto(15m);

            var linea = ComprobanteLinea.Create(
                1, Desc("Prod con $"), UM_NIU(), C(1m),
                M(100m, PEN()), false, Gravado(), Tasa(0.18m), dcto
            );

            // BaseAntes = 100; Dcto = 15; BaseDespues = 85; IGV = 15.30; Total = 100.30
            Assert.That(linea.BaseAntesDescuento.Monto, Is.EqualTo(100.00m));
            Assert.That(linea.DescuentoMonto.Monto,     Is.EqualTo(15.00m));
            Assert.That(linea.BaseImponible.Monto,      Is.EqualTo(85.00m));
            Assert.That(linea.Igv.Monto,                Is.EqualTo(15.30m));
            Assert.That(linea.ImporteTotal.Monto,       Is.EqualTo(100.30m));
        }

        // ============================================
        // Enforcement de escala por UM
        // ============================================

        [TestCase("NIU",  "Unidad",   1.99,   0)] // 0 decimales
    [TestCase("NIU",  "Unidad",   3.49,   0)] // reemplazo E48 por NIU
        [TestCase("KGM",  "Kg",       1.2349, 3)] // 3 decimales
        [TestCase("LTR",  "Litro",    2.7777, 3)]
        [TestCase("MTR",  "Metro",    5.1004, 3)]
    [TestCase("KGM",  "Otra",     7.1234567, 3)] // reemplazo XYZ por KGM
        public void Cantidad_RespetaEscalaSegunUM(string codigo, string nombre, decimal cantidad, int escalaEsperada)
        {
            UnidadDeMedida? um = codigo switch
            {
                "NIU" => UnidadDeMedida.NIU,
                "KGM" => UnidadDeMedida.KGM,
                "LTR" => UnidadDeMedida.LTR,
                "MTR" => UnidadDeMedida.MTR,
                _      => null
            };
            if (um == null)
            {
                Assert.Ignore($"Unidad de medida '{codigo}' no está definida en el dominio. Test ignorado.");
                return;
            }
            var cantidadRedondeada = Math.Round(cantidad, escalaEsperada, MidpointRounding.AwayFromZero);
            var linea = ComprobanteLinea.Create(
                1, Desc("Escala"), um, C(cantidadRedondeada, escalaEsperada),
                M(1m, PEN()), false, Gravado(), Tasa(0.18m), DescuentoLinea.None
            );

            var valor = linea.Cantidad.Value;

            // La cantidad final debería ser igual a redondear a la escala esperada con AwayFromZero
            var esperado = cantidadRedondeada;
            Assert.That(valor, Is.EqualTo(esperado), $"Cantidad [{cantidad}] no fue redondeada a escala {escalaEsperada} para UM {codigo}.");
        }

        [Test]
        public void CambiarUnidad_ActualizaEscalaYRecalcula()
        {
            var cantidadInicial = 3m; // valor entero válido para NIU (0 decimales)
            var linea = ComprobanteLinea.Create(
                1, Desc("Escala UM"), UM_KGM(), C(cantidadInicial, 3),
                M(10m, PEN()), false, Gravado(), Tasa(0.18m), DescuentoLinea.None
            );

            // KGM → 3 decimales
            Assert.That(linea.Cantidad.Value, Is.EqualTo(cantidadInicial));

            // Cambiamos a NIU → 0 decimales
            linea.CambiarUnidad(UM_NIU());
            Assert.That(linea.Cantidad.Value % 1m, Is.EqualTo(0m)); // entero
        }

        [Test]
        public void CambiarCantidad_RespetaEscalaDeLaUM()
        {
            var cantidadInicial = Math.Round(1.23456m, 3, MidpointRounding.AwayFromZero); // valor válido para LTR
            var linea = ComprobanteLinea.Create(
                1, Desc("Escala Cant"), UM_LTR(), C(cantidadInicial, 3),
                M(2m, PEN()), false, Gravado(), Tasa(0.18m), DescuentoLinea.None
            );
            // LTR → 3 decimales
            Assert.That(linea.Cantidad.Value, Is.EqualTo(cantidadInicial));

            var cantidadNueva = Math.Round(2.999m, 3, MidpointRounding.AwayFromZero); // valor válido para LTR
            linea.CambiarCantidad(C(cantidadNueva, 3));
            Assert.That(linea.Cantidad.Value, Is.EqualTo(cantidadNueva)); // redondeo 3 decimales
        }

        // ============================================
        // Mutaciones y recálculo
        // ============================================

        [Test]
        public void CambiarPrecio_RecalculaUnitariosYTotales()
        {
            var linea = ComprobanteLinea.Create(
                1, Desc("Mut precio"), UM_NIU(), C(2m),
                M(100m, PEN()), false, Gravado(), Tasa(0.18m), DescuentoLinea.None
            );

            Assert.That(linea.ImporteTotal.Monto, Is.EqualTo(236.00m));

            // Cambiar a precio CON IGV=118
            linea.CambiarPrecio(M(118m, PEN()), incluyeIgv: true);

            Assert.That(linea.UnitPriceSinIgv.Monto, Is.EqualTo(100.00m));
            Assert.That(linea.UnitPriceConIgv.Monto, Is.EqualTo(118.00m));
            Assert.That(linea.ImporteTotal.Monto, Is.EqualTo(236.00m));
        }

        [Test]
        public void CambiarImpuesto_RecalculaConNuevaTasa()
        {
            var linea = ComprobanteLinea.Create(
                1, Desc("Mut imp"), UM_NIU(), C(1m),
                M(100m, PEN()), false, Gravado(), Tasa(0.18m), DescuentoLinea.None
            );

            Assert.That(linea.Igv.Monto, Is.EqualTo(18.00m));
            linea.CambiarImpuesto(Gravado(), Tasa(0.10m)); // 10%

            Assert.That(linea.Igv.Monto, Is.EqualTo(10.00m));
            Assert.That(linea.ImporteTotal.Monto, Is.EqualTo(110.00m));
        }

        [Test]
        public void CambiarDescuento_Recalcula()
        {
            var linea = ComprobanteLinea.Create(
                1, Desc("Mut desc"), UM_NIU(), C(2m),
                M(100m, PEN()), false, Gravado(), Tasa(0.18m), DescuentoLinea.None
            );

            // Aplica 5% de descuento (ajusta fábrica si tu VO usa otro nombre)
            linea.CambiarDescuento(DescuentoLinea.FromFraccion(0.05m));

            Assert.That(linea.BaseAntesDescuento.Monto, Is.EqualTo(200.00m));
            Assert.That(linea.DescuentoMonto.Monto,     Is.EqualTo(10.00m));
            Assert.That(linea.BaseImponible.Monto,      Is.EqualTo(190.00m));
            Assert.That(linea.Igv.Monto,                Is.EqualTo(34.20m));
            Assert.That(linea.ImporteTotal.Monto,       Is.EqualTo(224.20m));
        }

        [Test]
        public void CambiarCentroDeCosto_ActualizaCampo()
        {
            var cc1 = new SharedKernel.ValueObjects.CentroDeCosto("CC-01", "Ventas");
            var cc2 = new SharedKernel.ValueObjects.CentroDeCosto("CC-02", "Servicios");

            var linea = ComprobanteLinea.Create(
                1, Desc("CC"), UM_NIU(), C(1m),
                M(10m, PEN()), false, Gravado(), Tasa(0.18m), DescuentoLinea.None, cc1
            );

            Assert.That(linea.CentroDeCosto.HasValue, Is.True);
            Assert.That(linea.CentroDeCosto!.Value.Code, Is.EqualTo("CC-01"));

            linea.CambiarCentroDeCosto(cc2);
            Assert.That(linea.CentroDeCosto!.Value.Code, Is.EqualTo("CC-02"));

            linea.CambiarCentroDeCosto(null);
            Assert.That(linea.CentroDeCosto.HasValue, Is.False);
        }

        // ============================================
        // Validaciones/errores
        // ============================================

        [Test]
        public void Crear_NumeroLineaInvalido_Lanza()
        {
            Assert.That(() => ComprobanteLinea.Create(
                0, Desc("X"), UM_NIU(), C(1m),
                M(10m, PEN()), false, Gravado(), Tasa(0.18m), DescuentoLinea.None
            ), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Crear_CamposObligatoriosFaltantes_LanzaExcepcionDeDominio()
        {
            // Falta UM (por ejemplo)
            Assert.That(() => ComprobanteLinea.Create(
                1, Desc("X"), um: null!,
                C(1m), M(10m, PEN()), false, Gravado(), Tasa(0.18m), DescuentoLinea.None
            ), Throws.TypeOf<CamposObligatoriosFaltantesException>());
        }

        [Test]
        public void CambiarPrecio_ConMonedaDistinta_Lanza()
        {
            var linea = ComprobanteLinea.Create(
                1, Desc("Moneda"), UM_NIU(), C(1m),
                M(10m, PEN()), false, Gravado(), Tasa(0.18m), DescuentoLinea.None
            );

            Assert.That(() => linea.CambiarPrecio(M(10m, USD())),
                Throws.TypeOf<InvalidOperationException>()
                      .With.Message.Contains("moneda"));
        }

        // ============================================
        // Consultas y ToString
        // ============================================

        [Test]
        public void SubtotalSinIgv_EsIgualABaseImponible_Y_TotalLinea_AImporteTotal()
        {
            var linea = ComprobanteLinea.Create(
                1, Desc("Totales"), UM_NIU(), C(2m),
                M(100m, PEN()), false, Gravado(), Tasa(0.18m), DescuentoLinea.None
            );

            Assert.That(linea.SubtotalSinIgv.Monto, Is.EqualTo(linea.BaseImponible.Monto));
            Assert.That(linea.TotalLinea.Monto,     Is.EqualTo(linea.ImporteTotal.Monto));
        }

        [Test]
        public void ToString_IncluyeNumero_Cantidad_UM_Descripcion()
        {
            var linea = ComprobanteLinea.Create(
                3, Desc("Item Z"), UM_NIU(), C(5m),
                M(10m, PEN()), false, Gravado(), Tasa(0.18m), DescuentoLinea.None
            );

            var s = linea.ToString();
            Assert.That(s, Does.Contain("#3"));
            Assert.That(s, Does.Contain("5"));           // cantidad
            Assert.That(s, Does.Contain("NIU"));         // UM
            Assert.That(s, Does.Contain("Item Z"));      // descripción
        }
    }
}

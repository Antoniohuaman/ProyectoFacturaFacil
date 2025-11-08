using System;
using NUnit.Framework;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.UnitTests.ValueObjects
{
    [TestFixture]
    public class DescuentoLineaTests
    {
        // ------------------------
        // Creación y validaciones
        // ------------------------

        [Test]
        public void None_NoAplicaDescuento()
        {
            var d = DescuentoLinea.None;

            Assert.Multiple(() =>
            {
                Assert.That(d.EsNinguno, Is.True);
                Assert.That(d.EsPorcentaje, Is.False);
                Assert.That(d.EsMonto, Is.False);
                Assert.That(d.Modo, Is.EqualTo(DescuentoLineaModo.Ninguno));
                Assert.That(d.Valor, Is.EqualTo(0m));
                Assert.That(d.CalcularMontoSobreBase(200m), Is.EqualTo(0m));
            });
        }

        [Test]
        public void FromPorcentaje_Acepta0a100_ConvierteAFraccion()
        {
            var d10 = DescuentoLinea.FromPorcentaje(10m);
            var d0  = DescuentoLinea.FromPorcentaje(0m);
            var d100= DescuentoLinea.FromPorcentaje(100m);

            Assert.Multiple(() =>
            {
                Assert.That(d10.EsPorcentaje, Is.True);
                Assert.That(d10.Valor, Is.EqualTo(0.10m));
                Assert.That(d0.Valor,  Is.EqualTo(0.00m));
                Assert.That(d100.Valor,Is.EqualTo(1.00m));
            });
        }

        [Test]
        public void FromPorcentaje_FueraDeRango_Lanza()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => DescuentoLinea.FromPorcentaje(-0.01m));
            Assert.Throws<ArgumentOutOfRangeException>(() => DescuentoLinea.FromPorcentaje(100.01m));
        }

        [Test]
        public void FromFraccion_Acepta0a1()
        {
            var d = DescuentoLinea.FromFraccion(0.25m);
            Assert.That(d.EsPorcentaje, Is.True);
            Assert.That(d.Valor, Is.EqualTo(0.25m));
        }

        [Test]
        public void FromFraccion_FueraDeRango_Lanza()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => DescuentoLinea.FromFraccion(-0.0001m));
            Assert.Throws<ArgumentOutOfRangeException>(() => DescuentoLinea.FromFraccion(1.0001m));
        }

        [Test]
        public void FromMonto_AceptaCeroYPositivos_RechazaNegativos()
        {
            var d0 = DescuentoLinea.FromMonto(0m);
            var d  = DescuentoLinea.FromMonto(25.123m); // se redondea internamente a 2

            Assert.Multiple(() =>
            {
                Assert.That(d0.EsMonto, Is.True);
                Assert.That(d0.Valor, Is.EqualTo(0m));
                Assert.That(d.EsMonto, Is.True);
                Assert.That(d.Valor, Is.EqualTo(25.12m));
            });

            Assert.Throws<ArgumentOutOfRangeException>(() => DescuentoLinea.FromMonto(-1m));
        }

        [Test]
        public void CalcularMontoSobreBase_MontoMayorQueBase_Lanza()
        {
            var d = DescuentoLinea.FromMonto(250m);
            Assert.Throws<ComprobantesElectronicosBC.Domain.Exceptions.ReglaDeNegocioException>(() => d.CalcularMontoSobreBase(200m));
        }

        // -----------------------------------------
        // Integración con ImpuestoIGV (18% y 10%)
        // -----------------------------------------

        [Test]
        public void Aplicar_Porcentaje10_Gravado18_SinPrecioIncluidoIGV()
        {
            // Precio sin IGV: 100, Cantidad: 2, BaseAntes=200, IGVAntes=36, TotalAntes=236
            var afectacion = SharedKernel.ValueObjects.AfectacionImpuesto.From("10");
            var cant = Cantidad.Create(2m);
            var desc = DescuentoLinea.FromPorcentaje(10m); // 10%
            var tasa = SharedKernel.ValueObjects.TasaImpuesto.FromPercent(18m);
            var res = desc.Aplicar(afectacion, tasa, unitPriceEntrada: 100m, cantidad: cant, priceIncludesIgv: false);

            Assert.Multiple(() =>
            {
                Assert.That(res.BaseAntes,   Is.EqualTo(200.00m));
                Assert.That(res.Descuento,   Is.EqualTo(20.00m));
                Assert.That(res.BaseDespues, Is.EqualTo(180.00m));
                Assert.That(res.Igv,         Is.EqualTo(32.40m)); // 180 * 0.18
                Assert.That(res.Total,       Is.EqualTo(212.40m));
            });
        }

        [Test]
        public void Aplicar_Porcentaje10_Gravado18_PrecioIncluyeIGV()
        {
            // Precio con IGV: 118, Cantidad: 2 → unitSin≈100 → BaseAntes=200, TotalAntes=236
            var afectacion = SharedKernel.ValueObjects.AfectacionImpuesto.From("10");
            var cant = Cantidad.Create(2m);
            var desc = DescuentoLinea.FromPorcentaje(10m);
            var tasa = SharedKernel.ValueObjects.TasaImpuesto.FromPercent(18m);
            var res = desc.Aplicar(afectacion, tasa, unitPriceEntrada: 118m, cantidad: cant, priceIncludesIgv: true);

            // Debe dar el mismo resultado que el caso anterior
            Assert.Multiple(() =>
            {
                Assert.That(res.BaseAntes,   Is.EqualTo(200.00m));
                Assert.That(res.Descuento,   Is.EqualTo(20.00m));
                Assert.That(res.BaseDespues, Is.EqualTo(180.00m));
                Assert.That(res.Igv,         Is.EqualTo(32.40m));
                Assert.That(res.Total,       Is.EqualTo(212.40m));
            });
        }

        [Test]
        public void Aplicar_Monto25_Gravado10_SinPrecioIncluidoIGV()
        {
            // Precio sin IGV: 100, Cantidad: 2, BaseAntes=200, IGVAntes=20, TotalAntes=220
            var afectacion = SharedKernel.ValueObjects.AfectacionImpuesto.From("12"); // "12" para Gravado 10%
            var cant = Cantidad.Create(2m);
            var desc = DescuentoLinea.FromMonto(25m);
            var tasa = SharedKernel.ValueObjects.TasaImpuesto.FromPercent(10m);
            var res = desc.Aplicar(afectacion, tasa, unitPriceEntrada: 100m, cantidad: cant, priceIncludesIgv: false);

            // BaseDespues=175 → IGV=17.50 → Total=192.50
            Assert.Multiple(() =>
            {
                Assert.That(res.BaseAntes,   Is.EqualTo(200.00m));
                Assert.That(res.Descuento,   Is.EqualTo(25.00m));
                Assert.That(res.BaseDespues, Is.EqualTo(175.00m));
                Assert.That(res.Igv,         Is.EqualTo(17.50m)); // 175 * 0.10
                Assert.That(res.Total,       Is.EqualTo(192.50m));
            });
        }

        [Test]
        public void Aplicar_100PorCiento_Gravado18_DejaEnCero()
        {
            var afectacion = SharedKernel.ValueObjects.AfectacionImpuesto.From("10");
            var cant = Cantidad.Create(2m);
            var desc = DescuentoLinea.FromPorcentaje(100m);
            var tasa = SharedKernel.ValueObjects.TasaImpuesto.FromPercent(18m);
            var res = desc.Aplicar(afectacion, tasa, unitPriceEntrada: 100m, cantidad: cant, priceIncludesIgv: false);

            Assert.Multiple(() =>
            {
                Assert.That(res.BaseAntes,   Is.EqualTo(200.00m));
                Assert.That(res.Descuento,   Is.EqualTo(200.00m));
                Assert.That(res.BaseDespues, Is.EqualTo(0.00m));
                Assert.That(res.Igv,         Is.EqualTo(0.00m));
                Assert.That(res.Total,       Is.EqualTo(0.00m));
            });
        }

        [Test]
        public void Aplicar_Porcentaje10_Exonerado_IgvCero()
        {
            var afectacion = SharedKernel.ValueObjects.AfectacionImpuesto.From("20"); // "20" para Exonerado
            var cant = Cantidad.Create(3m);

            // Precio unitario “base” 50, cantidad 3 → BaseAntes=150
            var desc = DescuentoLinea.FromPorcentaje(10m);
            var tasa = SharedKernel.ValueObjects.TasaImpuesto.FromPercent(0m);
            var res = desc.Aplicar(afectacion, tasa, unitPriceEntrada: 50m, cantidad: cant, priceIncludesIgv: false);

            Assert.Multiple(() =>
            {
                Assert.That(res.BaseAntes,   Is.EqualTo(150.00m));
                Assert.That(res.Descuento,   Is.EqualTo(15.00m));
                Assert.That(res.BaseDespues, Is.EqualTo(135.00m));
                Assert.That(res.Igv,         Is.EqualTo(0.00m));
                Assert.That(res.Total,       Is.EqualTo(135.00m));
            });
        }

        // --------------------------
        // ToAllowanceCharge (UBL)
        // --------------------------

        [Test]
        public void ToAllowanceCharge_DesdePorcentaje_IncluyeFactor()
        {
            var d = DescuentoLinea.FromPorcentaje(10m);
            var ac = d.ToAllowanceCharge(baseAntes: 200m);

            Assert.Multiple(() =>
            {
                Assert.That(ac.ChargeIndicator, Is.False);
                Assert.That(ac.BaseAmount, Is.EqualTo(200.00m));
                Assert.That(ac.Amount, Is.EqualTo(20.00m));
                Assert.That(ac.MultiplierFactorNumeric, Is.EqualTo(0.10m));
                Assert.That(ac.ChargeReasonCode, Is.EqualTo("00"));
            });
        }

        [Test]
        public void ToAllowanceCharge_DesdeMonto_NoIncluyeFactor()
        {
            var d = DescuentoLinea.FromMonto(25m);
            var ac = d.ToAllowanceCharge(baseAntes: 200m);

            Assert.Multiple(() =>
            {
                Assert.That(ac.ChargeIndicator, Is.False);
                Assert.That(ac.BaseAmount, Is.EqualTo(200.00m));
                Assert.That(ac.Amount, Is.EqualTo(25.00m));
                Assert.That(ac.MultiplierFactorNumeric, Is.Null);
                Assert.That(ac.ChargeReasonCode, Is.EqualTo("00"));
            });
        }
    }
}

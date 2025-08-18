using System;
using NUnit.Framework;
using SharedKernel.ValueObjects;

namespace SharedKernel.ValueObjects.Tests
{
    [TestFixture]
    public class TasaImpuestoTests
    {
        // --------- FromPercent ---------

        [TestCase(0,   0.00)]
        [TestCase(8,   0.08)]
        [TestCase(10,  0.10)]
        [TestCase(12,  0.12)]
        [TestCase(18,  0.18)]
        [TestCase(100, 1.00)]
        public void FromPercent_ConvierteYNormaliza(decimal porcentaje, decimal fraccionEsperada)
        {
            var t = TasaImpuesto.FromPercent(porcentaje);
            Assert.Multiple(() =>
            {
                Assert.That(t.Fraccion,  Is.EqualTo((decimal)fraccionEsperada));
                Assert.That(t.Porcentaje,Is.EqualTo(porcentaje));
            });
        }

        [TestCase(-0.01)]
        [TestCase(100.01)]
        public void FromPercent_FueraDeRango_Lanza(decimal porcentajeFuera)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TasaImpuesto.FromPercent(porcentajeFuera));
        }

        // --------- FromFraction ---------

        [TestCase(0.00, 0)]
        [TestCase(0.08, 8)]
        [TestCase(0.10, 10)]
        [TestCase(0.12, 12)]
        [TestCase(0.18, 18)]
        [TestCase(1.00, 100)]
        public void FromFraction_ConvierteYNormaliza(decimal fraccion, decimal porcentajeEsperado)
        {
            var t = TasaImpuesto.FromFraction(fraccion);
            Assert.Multiple(() =>
            {
                Assert.That(t.Fraccion,  Is.EqualTo(fraccion));
                Assert.That(t.Porcentaje,Is.EqualTo(porcentajeEsperado));
            });
        }

        [TestCase(-0.000001)]
        [TestCase(1.000001)]
        public void FromFraction_FueraDeRango_Lanza(decimal fraccionFuera)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TasaImpuesto.FromFraction(fraccionFuera));
        }

        [Test]
        public void Normalizacion_A6Decimales()
        {
            // 0.12345678 -> 0.123457 (away-from-zero)
            var t = TasaImpuesto.FromFraction(0.12345678m);
            Assert.That(t.Fraccion, Is.EqualTo(0.123457m));
            Assert.That(t.ToString(), Is.EqualTo("0.123457"));  // formato 0.######
        }

        // --------- Helpers de formato ---------

        [Test]
        public void ToPercentString_FormateaConDecimales()
        {
            var t = TasaImpuesto.FromPercent(10);
            Assert.That(t.ToPercentString(),   Is.EqualTo("10.00%"));
            Assert.That(t.ToPercentString(3),  Is.EqualTo("10.000%"));
        }

        [Test]
        public void ToDisplay_DevuelveEtiquetaParaUI()
        {
            var t = TasaImpuesto.IGV18;
            Assert.That(t.ToDisplay("IGV"), Is.EqualTo("IGV (18.00%)"));
            Assert.That(t.ToDisplay("IVA"), Is.EqualTo("IVA (18.00%)"));
        }

        // --------- CompatibilizarCon(Afectacion) ---------

        [Test]
        public void CompatibilizarCon_NoGravado_RetornaCero()
        {
            var tasa = TasaImpuesto.IGV18;

            foreach (var code in new[] { "20","21","30","31","32","33","34","35","36","40" })
            {
                var afect = AfectacionImpuesto.From(code);
                var t = tasa.CompatibilizarCon(afect);
                Assert.That(t, Is.EqualTo(TasaImpuesto.Cero), $"Código {code} debería forzar 0%");
            }
        }

        [Test]
        public void CompatibilizarCon_GravadoOIVAP_ConservaLaTasa()
        {
            foreach (var code in new[] { "10","11","12","13","14","15","16","17" })
            {
                var afect = AfectacionImpuesto.From(code);
                var t10   = TasaImpuesto.IGV10.CompatibilizarCon(afect);
                var t18   = TasaImpuesto.IGV18.CompatibilizarCon(afect);

                Assert.That(t10, Is.EqualTo(TasaImpuesto.IGV10), $"Código {code} debería mantener 10%");
                Assert.That(t18, Is.EqualTo(TasaImpuesto.IGV18), $"Código {code} debería mantener 18%");
            }
        }

        // --------- Constantes útiles ---------

        [Test]
        public void Constantes_ValoresCorrectos()
        {
            Assert.Multiple(() =>
            {
                Assert.That(TasaImpuesto.Cero.Fraccion,  Is.EqualTo(0m));
                Assert.That(TasaImpuesto.IGV8.Fraccion,   Is.EqualTo(0.08m));
                Assert.That(TasaImpuesto.IGV10.Fraccion,  Is.EqualTo(0.10m));
                Assert.That(TasaImpuesto.IGV12.Fraccion,  Is.EqualTo(0.12m));
                Assert.That(TasaImpuesto.IGV18.Fraccion,  Is.EqualTo(0.18m));
            });
        }

        // --------- TryFrom* ---------

        [Test]
        public void TryFromPercent_Válido()
        {
            var ok = TasaImpuesto.TryFromPercent(10, out var t);
            Assert.Multiple(() =>
            {
                Assert.That(ok, Is.True);
                Assert.That(t,  Is.Not.Null);
                Assert.That(t!.Fraccion, Is.EqualTo(0.10m));
            });
        }

        [Test]
        public void TryFromPercent_Invalido()
        {
            var ok = TasaImpuesto.TryFromPercent(120, out var t);
            Assert.Multiple(() =>
            {
                Assert.That(ok, Is.False);
                Assert.That(t,  Is.Null);
            });
        }

        [Test]
        public void TryFromFraction_Válido()
        {
            var ok = TasaImpuesto.TryFromFraction(0.18m, out var t);
            Assert.Multiple(() =>
            {
                Assert.That(ok, Is.True);
                Assert.That(t,  Is.Not.Null);
                Assert.That(t!.Porcentaje, Is.EqualTo(18m));
            });
        }

        [Test]
        public void TryFromFraction_Invalido()
        {
            var ok = TasaImpuesto.TryFromFraction(1.5m, out var t);
            Assert.Multiple(() =>
            {
                Assert.That(ok, Is.False);
                Assert.That(t,  Is.Null);
            });
        }

        // --------- Igualdad por valor ---------

        [Test]
        public void IgualdadPorValor()
        {
            var a = TasaImpuesto.FromPercent(10);
            var b = TasaImpuesto.IGV10;
            var c = TasaImpuesto.IGV12;

            Assert.Multiple(() =>
            {
                Assert.That(a, Is.EqualTo(b));
                Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
                Assert.That(a, Is.Not.EqualTo(c));
            });
        }

        // --------- EsCero ---------

        [Test]
        public void EsCero_TrueSoloEnCero()
        {
            Assert.That(TasaImpuesto.Cero.EsCero, Is.True);
            Assert.That(TasaImpuesto.IGV10.EsCero, Is.False);
        }
    }
}
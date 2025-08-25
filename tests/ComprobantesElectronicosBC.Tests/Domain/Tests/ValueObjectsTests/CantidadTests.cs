using System;
using System.Globalization;
using NUnit.Framework;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.UnitTests.ValueObjects
{
    [TestFixture]
    public class CantidadTests
    {
        // --- Create() ---

        [TestCase(1)]
        [TestCase(2.5)]
        [TestCase(0.123456)] // exactamente 6 decimales: permitido con maxScale por defecto (6)
        public void Create_ValorValido_RegresaCantidad(decimal raw)
        {
            var c = Cantidad.Create(raw);
            Assert.That(c.Value, Is.EqualTo(raw));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-0.01)]
        public void Create_NoPositivo_Lanza(decimal raw)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Cantidad.Create(raw));
            Assert.That(ex!.ParamName, Is.EqualTo("value"));
        }

        [Test]
        public void Create_MasDe6Decimales_Lanza()
        {
            // 7 decimales → invalida con maxScale=6
            var ex = Assert.Throws<ArgumentException>(() => Cantidad.Create(0.1234567m));
            Assert.That(ex!.Message, Does.Contain("más de 6 decimales"));
        }

        [Test]
        public void Create_RespetaMaxScalePersonalizado()
        {
            // Con maxScale=3, 4 decimales debe fallar
            var ex = Assert.Throws<ArgumentException>(() => Cantidad.Create(1.2345m, maxScale: 3));
            Assert.That(ex!.Message, Does.Contain("3 decimales"));
        }

        // --- Uno ---

        [Test]
        public void Uno_Es1mYValido()
        {
            var uno = Cantidad.Uno;
            Assert.That(uno.Value, Is.EqualTo(1m));
        }

        // --- EnforceMaxScale() ---

        [Test]
        public void EnforceMaxScale_AdmiteEscalaDentroDelLimite()
        {
            var c = Cantidad.Create(1.234m);
            var same = c.EnforceMaxScale(3);
            Assert.That(same.Value, Is.EqualTo(1.234m));
        }

        [Test]
        public void EnforceMaxScale_ExcedeEscala_Lanza()
        {
            var c = Cantidad.Create(1.234m);
            var ex = Assert.Throws<ArgumentException>(() => c.EnforceMaxScale(2));
            Assert.That(ex!.Message, Does.Contain("precisión permitida"));
        }

        [Test]
        public void EnforceMaxScale_MaxScaleNegativo_Lanza()
        {
            var c = Cantidad.Create(1m);
            Assert.Throws<ArgumentOutOfRangeException>(() => c.EnforceMaxScale(-1));
        }

        // --- RoundTo() ---

        [Test]
        public void RoundTo_RedondeaAwayFromZeroPorDefecto()
        {
            // 1.2345 a 3 decimales -> 1.235 (AwayFromZero)
            var c = Cantidad.Create(1.2345m);
            var r = c.RoundTo(3);
            Assert.That(r.Value, Is.EqualTo(1.235m));
        }

        [Test]
        public void RoundTo_ScaleCero_RedondeaUnidad()
        {
            // 1.5 -> 2 (AwayFromZero)
            var c = Cantidad.Create(1.5m);
            var r = c.RoundTo(0);
            Assert.That(r.Value, Is.EqualTo(2m));
        }

        [Test]
        public void RoundTo_ScaleNegativa_Lanza()
        {
            var c = Cantidad.Create(1m);
            Assert.Throws<ArgumentOutOfRangeException>(() => c.RoundTo(-1));
        }

        // --- Igualdad por valor (record struct) ---

        [Test]
        public void Igualdad_MismoValor_True_Distinto_False()
        {
            var a = Cantidad.Create(2.5m);
            var b = Cantidad.Create(2.5m);
            var c = Cantidad.Create(2.500000m); // mismo valor
            var d = Cantidad.Create(2.6m);

            Assert.Multiple(() =>
            {
                Assert.That(a, Is.EqualTo(b));
                Assert.That(a, Is.EqualTo(c));
                Assert.That(a, Is.Not.EqualTo(d));
            });
        }

        // --- ToString (Invariante) ---

        [Test]
        public void ToString_UsaCulturaInvariante()
        {
            var c1 = Cantidad.Create(2m);
            var c2 = Cantidad.Create(2.5m);

            // El VO usa CultureInfo.InvariantCulture internamente
            Assert.Multiple(() =>
            {
                Assert.That(c1.ToString(), Is.EqualTo("2"));
                Assert.That(c2.ToString(), Is.EqualTo("2.5"));
            });
        }

        // --- Caso típico NIU (sin decimales) ---

        [Test]
        public void EnforceMaxScale_NIU_NoPermiteDecimales()
        {
            var c = Cantidad.Create(1m);
            Assert.DoesNotThrow(() => c.EnforceMaxScale(0));
            var ex = Assert.Throws<ArgumentException>(() => Cantidad.Create(1.1m).EnforceMaxScale(0));
            Assert.That(ex!.Message, Does.Contain("precisión permitida (0 decimales)"));
        }
    }
}

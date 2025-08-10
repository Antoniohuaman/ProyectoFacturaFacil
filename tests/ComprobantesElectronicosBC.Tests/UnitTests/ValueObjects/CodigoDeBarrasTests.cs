using System;
using NUnit.Framework;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.UnitTests.ValueObjects
{
    public class CodigoDeBarrasTests
    {
        // Ejemplos válidos conocidos
        // EAN-13 (ISBN-13 clásico)
        private const string ValidEan13 = "9780306406157";
        // UPC-A (verificado con cálculo DV)
        private const string ValidUpcA  = "036000291452";
        // EAN-8: calculado DV para "1234567" -> 0
        private const string ValidEan8  = "12345670";

        [Test]
        public void CreateEan13_ValidaDV_Y_AsignaPropiedades()
        {
            var cb = CodigoDeBarras.CreateEan13(ValidEan13);
            Assert.Multiple(() =>
            {
                Assert.That(cb.Tipo,  Is.EqualTo(CodigoDeBarras.EAN13));
                Assert.That(cb.Valor, Is.EqualTo(ValidEan13));
                Assert.That(cb.Mostrar, Is.EqualTo(ValidEan13));
            });
        }

        [Test]
        public void CreateEan13_DVIncorrecto_Lanza()
        {
            var invalido = ValidEan13[..12] + "0"; // cambia el dígito verificador
            var ex = Assert.Throws<ArgumentException>(() => CodigoDeBarras.CreateEan13(invalido));
            Assert.That(ex!.Message, Does.Contain("EAN-13"));
        }

        [Test]
        public void CreateUpcA_Valido()
        {
            var cb = CodigoDeBarras.CreateUpcA(ValidUpcA);
            Assert.Multiple(() =>
            {
                Assert.That(cb.Tipo,  Is.EqualTo(CodigoDeBarras.UPCA));
                Assert.That(cb.Valor, Is.EqualTo(ValidUpcA));
            });
        }

        [Test]
        public void CreateUpcA_DVIncorrecto_Lanza()
        {
            var invalido = ValidUpcA[..11] + "0";
            var ex = Assert.Throws<ArgumentException>(() => CodigoDeBarras.CreateUpcA(invalido));
            Assert.That(ex!.Message, Does.Contain("UPC-A"));
        }

        [Test]
        public void CreateEan8_Valido()
        {
            var cb = CodigoDeBarras.CreateEan8(ValidEan8);
            Assert.Multiple(() =>
            {
                Assert.That(cb.Tipo,  Is.EqualTo(CodigoDeBarras.EAN8));
                Assert.That(cb.Valor, Is.EqualTo(ValidEan8));
            });
        }

        [Test]
        public void CreateEan8_DVIncorrecto_Lanza()
        {
            var invalido = ValidEan8[..7] + "9";
            var ex = Assert.Throws<ArgumentException>(() => CodigoDeBarras.CreateEan8(invalido));
            Assert.That(ex!.Message, Does.Contain("EAN-8"));
        }

        [Test]
        public void CreateCode128_AceptaAsciiImprimible_Y_LongitudDentroDelLimite()
        {
            var txt = "ABC-123 / Caja #1";
            var cb = CodigoDeBarras.CreateCode128(txt);
            Assert.Multiple(() =>
            {
                Assert.That(cb.Tipo,  Is.EqualTo(CodigoDeBarras.CODE128));
                Assert.That(cb.Valor, Is.EqualTo(txt.Trim()));
            });
        }

        [Test]
        public void CreateCode128_RechazaNoImprimibles()
        {
            var ex = Assert.Throws<ArgumentException>(() => CodigoDeBarras.CreateCode128("Hola\tMundo"));
            Assert.That(ex!.Message, Does.Contain("CODE128").IgnoreCase);
        }

        [Test]
        public void FromScan_Autodetecta_Tipos_Numéricos()
        {
            var e13 = CodigoDeBarras.FromScan(ValidEan13);
            var upc = CodigoDeBarras.FromScan(ValidUpcA);
            var e8  = CodigoDeBarras.FromScan(ValidEan8);

            Assert.Multiple(() =>
            {
                Assert.That(e13.Tipo, Is.EqualTo(CodigoDeBarras.EAN13));
                Assert.That(upc.Tipo, Is.EqualTo(CodigoDeBarras.UPCA));
                Assert.That(e8.Tipo,  Is.EqualTo(CodigoDeBarras.EAN8));
            });
        }

        [Test]
        public void FromScan_Autodetecta_Code128_CuandoNoSonSoloDigitos()
        {
            var cb = CodigoDeBarras.FromScan("P-000123-A");
            Assert.That(cb.Tipo, Is.EqualTo(CodigoDeBarras.CODE128));
        }

        [Test]
        public void FromScan_NumeroConLongitudInvalida_Lanza()
        {
            // 10 dígitos no corresponde a EAN8/UPCA/EAN13
            var ex = Assert.Throws<ArgumentException>(() => CodigoDeBarras.FromScan("1234567890"));
            Assert.That(ex!.Message, Does.Contain("Longitud numérica inválida"));
        }

        [Test]
        public void TryFromScan_DevuelveFalse_EnError()
        {
            var ok = CodigoDeBarras.TryFromScan(ValidEan13, out var a);
            var bad = CodigoDeBarras.TryFromScan("123", out var b);

            Assert.Multiple(() =>
            {
                Assert.That(ok, Is.True);
                Assert.That(a, Is.Not.Null);
                Assert.That(bad, Is.False);
                Assert.That(b, Is.Null);
            });
        }

        [Test]
        public void IgualdadPorValor_MismoTipoYValor_EsIgual()
        {
            var x = CodigoDeBarras.CreateEan13(ValidEan13);
            var y = CodigoDeBarras.CreateEan13(ValidEan13);
            Assert.That(x, Is.EqualTo(y));
        }

        [Test]
        public void ToString_IncluyeTipoYValor()
        {
            var cb = CodigoDeBarras.CreateUpcA(ValidUpcA);
            var s = cb.ToString();
            Assert.Multiple(() =>
            {
                Assert.That(s, Does.Contain(CodigoDeBarras.UPCA));
                Assert.That(s, Does.Contain(ValidUpcA));
            });
        }
    }
}

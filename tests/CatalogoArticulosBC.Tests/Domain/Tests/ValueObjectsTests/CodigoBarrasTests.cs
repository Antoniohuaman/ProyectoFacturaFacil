using System;
using NUnit.Framework;
using CatalogoArticulosBC.Domain.ValueObjects;

namespace CatalogoArticulosBC.Domain.Tests.ValueObjects
{
    [TestFixture]
    public class CodigoBarrasTests
    {
        // Ejemplos con check digit válido (precalculados)
        private const string GTIN8   = "78035683";
        private const string GTIN12  = "038270167178";   // UPC-A
        private const string GTIN13  = "1511517724901";  // EAN-13
        private const string GTIN14  = "11234567890125"; // Empieza con 1 (uso logístico)

        [Test]
        public void Ctor_Null_O_Vacio_DejaNull()
        {
            var a = new CodigoBarras(null);
            var b = new CodigoBarras("   ");

            Assert.That(a.Valor, Is.Null);
            Assert.That(b.Valor, Is.Null);
        }

        [Test]
        public void Ctor_Acepta_GTIN8_12_13_14()
        {
            Assert.That(new CodigoBarras(GTIN8).Valor,  Is.EqualTo(GTIN8));
            Assert.That(new CodigoBarras(GTIN12).Valor, Is.EqualTo(GTIN12));
            Assert.That(new CodigoBarras(GTIN13).Valor, Is.EqualTo(GTIN13));
            Assert.That(new CodigoBarras(GTIN14).Valor, Is.EqualTo(GTIN14));
        }

        [Test]
        public void Normaliza_QuitaEspaciosYGuiones()
        {
            var conRuido = $"  {GTIN13[..4]}-{GTIN13[4..8]}-{GTIN13[8..]}  ";

            var c = new CodigoBarras(conRuido);

            Assert.That(c.Valor, Is.EqualTo(GTIN13));
        }

        [Test]
        public void Ctor_Rechaza_LongitudInvalida()
        {
            var nueve = "123456789"; // 9 dígitos

            TestDelegate act = () => _ = new CodigoBarras(nueve);

            Assert.That(act, Throws.TypeOf<ArgumentException>()
                .With.Property("ParamName").EqualTo("valor")
                .And.Message.Contains("longitud"));
        }

        [Test]
        public void Ctor_Rechaza_NoNumerico()
        {
            TestDelegate act = () => _ = new CodigoBarras("ABC12345");

            Assert.That(act, Throws.TypeOf<ArgumentException>()
                .With.Property("ParamName").EqualTo("valor")
                .And.Message.Contains("solo dígitos"));
        }

        [Test]
        public void Ctor_Rechaza_CheckDigitInvalido()
        {
            var invalido = GTIN13[..^1] + (GTIN13[^1] == '0' ? '1' : '0'); // cambia último dígito

            TestDelegate act = () => _ = new CodigoBarras(invalido);

            Assert.That(act, Throws.TypeOf<ArgumentException>()
                .With.Property("ParamName").EqualTo("valor")
                .And.Message.Contains("verificador"));
        }

        [Test]
        public void Igualdad_PorValor_Normalizado()
        {
            var a = new CodigoBarras(GTIN12);
            var b = new CodigoBarras($" {GTIN12[..6]}-{GTIN12[6..]} ");

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }
    }
}

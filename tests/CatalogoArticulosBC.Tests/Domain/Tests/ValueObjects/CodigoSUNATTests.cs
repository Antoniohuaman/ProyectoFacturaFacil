using System;
using NUnit.Framework;
using CatalogoArticulosBC.Domain.ValueObjects;

namespace CatalogoArticulosBC.Domain.Tests.ValueObjects
{
    [TestFixture]
    public class CodigoSUNATTests
    {
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Ctor_NullOVacio_DejaValorNull(string? input)
        {
            var codigo = new CodigoSUNAT(input);

            Assert.That(codigo.Valor, Is.Null);
            Assert.That(codigo.ToString(), Is.EqualTo(string.Empty));
        }

        [Test]
        public void Ctor_Acepta_OchoDigitos_Trim()
        {
            var codigo = new CodigoSUNAT("  95141706  "); // p. ej. BODEGA (UNSPSC)

            Assert.That(codigo.Valor, Is.EqualTo("95141706"));
            Assert.That(codigo.ToString(), Is.EqualTo("95141706"));
        }

        [Test]
        public void Ctor_Rechaza_LongitudDistintaDe8()
        {
            TestDelegate siete = () => _ = new CodigoSUNAT("1234567");
            TestDelegate nueve = () => _ = new CodigoSUNAT("123456789");

            Assert.That(siete, Throws.TypeOf<ArgumentException>()
                .With.Property("ParamName").EqualTo("valor"));
            Assert.That(nueve, Throws.TypeOf<ArgumentException>()
                .With.Property("ParamName").EqualTo("valor"));
        }

        [Test]
        public void Ctor_Rechaza_NoNumerico()
        {
            TestDelegate act = () => _ = new CodigoSUNAT("12A4567B");

            Assert.That(act, Throws.TypeOf<ArgumentException>()
                .With.Property("ParamName").EqualTo("valor"));
        }

        [Test]
        public void Igualdad_PorValor_Normalizado()
        {
            var a = new CodigoSUNAT("95141606");
            var b = new CodigoSUNAT(" 95141606 ");

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void Equals_ContraNull_EsFalso()
        {
            var a = new CodigoSUNAT("53111601");
            Assert.That(a.Equals(null), Is.False);
        }
    }
}

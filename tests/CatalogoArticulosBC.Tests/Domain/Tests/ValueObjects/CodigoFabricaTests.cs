using System;
using NUnit.Framework;
using CatalogoArticulosBC.Domain.ValueObjects;

namespace CatalogoArticulosBC.Domain.Tests.ValueObjects
{
    [TestFixture]
    public class CodigoFabricaTests
    {
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\t\r\n")]
        public void Ctor_NullOVacioOBlancos_DejaValorNull(string? input)
        {
            var codigo = new CodigoFabrica(input);

            Assert.That(codigo.Valor, Is.Null);
            Assert.That(codigo.ToString(), Is.Null);
        }

        [Test]
        public void Ctor_CuandoTieneContenido_AlmacenaTrim()
        {
            var codigo = new CodigoFabrica("  ABC-123  ");

            Assert.That(codigo.Valor, Is.EqualTo("ABC-123"));
            Assert.That(codigo.ToString(), Is.EqualTo("ABC-123"));
        }

        [Test]
        public void Igualdad_PorValor_NormalizandoTrim()
        {
            var a = new CodigoFabrica("  XYZ-999 ");
            var b = new CodigoFabrica("XYZ-999");

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void Igualdad_CuandoAmbosNull_EsVerdadera()
        {
            var a = new CodigoFabrica(null);
            var b = new CodigoFabrica("   ");

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode())); // ambos 0
        }

        [Test]
        public void Igualdad_EsCaseSensitive()
        {
            var lower = new CodigoFabrica("abc-123");
            var upper = new CodigoFabrica("ABC-123");

            Assert.That(lower.Equals(upper), Is.False);
            Assert.That(lower, Is.Not.EqualTo(upper));
        }

        [Test]
        public void Equals_ContraNull_EsFalso()
        {
            var code = new CodigoFabrica("ABC");
            Assert.That(code.Equals(null), Is.False);
        }

        [Test]
        public void MantieneEspaciosInternos_SoloAplicaTrimExterno()
        {
            var code = new CodigoFabrica("  A B C  ");
            Assert.That(code.Valor, Is.EqualTo("A B C")); // no se eliminan espacios intermedios
        }
    }
}

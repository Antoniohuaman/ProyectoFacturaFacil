using System;
using NUnit.Framework;
using CatalogoArticulosBC.Domain.ValueObjects;

namespace CatalogoArticulosBC.Domain.Tests.ValueObjects
{
    [TestFixture]
    public class MarcaTests
    {
        [Test]
        public void Ctor_CuandoEsValido_NormalizaTrimYMayusculas()
        {
            var marca = new Marca("  Nike  ");

            Assert.That(marca.Nombre, Is.EqualTo("NIKE"));
            Assert.That(marca.ToString(), Is.EqualTo("NIKE"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Ctor_CuandoEsNuloOVacio_LanzaArgumentException(string? input)
        {
            TestDelegate act = () => _ = new Marca(input!);

            Assert.That(act, Throws.TypeOf<ArgumentException>()
                .With.Property("ParamName").EqualTo("nombre"));
        }

        [Test]
        public void Ctor_CuandoExcede100Caracteres_LanzaArgumentException()
        {
            var demasiadoLargo = new string('a', 101);

            TestDelegate act = () => _ = new Marca(demasiadoLargo);

            Assert.That(act, Throws.TypeOf<ArgumentException>()
                .With.Property("ParamName").EqualTo("nombre"));
        }

        [Test]
        public void Ctor_CuandoEsExactamente100Caracteres_AceptaYNormaliza()
        {
            var exacto100 = new string('a', 100);

            var marca = new Marca(exacto100);

            Assert.That(marca.Nombre, Is.EqualTo(new string('A', 100)));
        }

        [Test]
        public void Igualdad_PorValor_CaseInsensitivePorNormalizacion()
        {
            var a = new Marca("coca-cola");
            var b = new Marca("  COCA-COLA  ");

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void Equals_ContraNull_EsFalso()
        {
            var a = new Marca("Acme");

            Assert.That(a.Equals(null), Is.False);
        }

        [Test]
        public void Normaliza_Acentos_ConInvariantCulture()
        {
            var m = new Marca("Lácteos");

            Assert.That(m.Nombre, Is.EqualTo("LÁCTEOS"));
        }

        [Test]
        public void PreservaEspaciosInternos_SoloTrimExterno()
        {
            var m = new Marca("  La  Costeña  ");

            Assert.That(m.Nombre, Is.EqualTo("LA  COSTEÑA"));
        }
    }
}

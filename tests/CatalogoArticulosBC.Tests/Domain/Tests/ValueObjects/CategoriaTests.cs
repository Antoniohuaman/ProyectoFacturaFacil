using System;
using NUnit.Framework;
using CatalogoArticulosBC.Domain.ValueObjects;

namespace CatalogoArticulosBC.Domain.Tests.ValueObjects
{
    [TestFixture]
    public class CategoriaTests
    {
        [Test]
        public void Ctor_CuandoEsValido_NormalizaTrimYMayusculas()
        {
            var categoria = new Categoria("  Gaseosas  ");

            Assert.That(categoria.Nombre, Is.EqualTo("GASEOSAS"));
            Assert.That(categoria.ToString(), Is.EqualTo("GASEOSAS"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Ctor_CuandoEsNuloOVacio_LanzaArgumentException(string? input)
        {
            TestDelegate act = () => _ = new Categoria(input!);

            Assert.That(act, Throws.TypeOf<ArgumentException>()
                .With.Property("ParamName").EqualTo("nombre"));
        }

        [Test]
        public void Ctor_CuandoExcede100Caracteres_LanzaArgumentException()
        {
            var demasiadoLargo = new string('a', 101);

            TestDelegate act = () => _ = new Categoria(demasiadoLargo);

            Assert.That(act, Throws.TypeOf<ArgumentException>()
                .With.Property("ParamName").EqualTo("nombre"));
        }

        [Test]
        public void Ctor_CuandoEsExactamente100Caracteres_AceptaYNormaliza()
        {
            var exacto100 = new string('a', 100);

            var categoria = new Categoria(exacto100);

            Assert.That(categoria.Nombre, Is.EqualTo(new string('A', 100)));
        }

        [Test]
        public void Igualdad_PorValor_Idempotente_YCaseInsensitivePorNormalizacion()
        {
            var a = new Categoria("gaseosas");
            var b = new Categoria("  GASEOSAS  ");

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void Equals_ContraNull_EsFalso()
        {
            var a = new Categoria("Gaseosas");

            Assert.That(a.Equals(null), Is.False);
        }

        [Test]
        public void Normaliza_Acentos_ConInvariantCulture()
        {
            var c = new Categoria("lácteos");

            Assert.That(c.Nombre, Is.EqualTo("LÁCTEOS"));
        }
    }
}

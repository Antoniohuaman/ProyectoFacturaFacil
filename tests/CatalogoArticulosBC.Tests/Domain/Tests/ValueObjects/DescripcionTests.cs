using System;
using NUnit.Framework;
using CatalogoArticulosBC.Domain.ValueObjects;

namespace CatalogoArticulosBC.Domain.Tests.ValueObjects
{
    [TestFixture]
    public class DescripcionTests
    {
        private const int Max = 500;

        [Test]
        public void From_NullDevuelveVacio_YNoLanza()
        {
            var d = Descripcion.From(null!); // null intencional

            Assert.That(d.Texto, Is.EqualTo(string.Empty));
            Assert.That(d.ToString(), Is.EqualTo(string.Empty));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\t\r\n")]
        public void From_EnBlanco_QuedaVacio(string input)
        {
            var d = Descripcion.From(input);

            Assert.That(d.Texto, Is.EqualTo(string.Empty));
        }

        [Test]
        public void From_AplicaTrim_PeroPreservaEspaciosInternos()
        {
            var d = Descripcion.From("  A  B  ");

            Assert.That(d.Texto, Is.EqualTo("A  B"));
        }

        [Test]
        public void From_PermiteMultilineas()
        {
            var texto = "Mochila escolar:\n- Color: verde\n- Para niños\n- Sin garantía exacta";
            var d = Descripcion.From("  " + texto + "  ");

            Assert.That(d.Texto, Is.EqualTo(texto));
        }

        [Test]
        public void From_Acepta_LongitudExacta500()
        {
            var exacto500 = new string('x', Max);

            var d = Descripcion.From(exacto500);

            Assert.That(d.Texto.Length, Is.EqualTo(Max));
        }

        [Test]
        public void From_LongitudMayorA500_LanzaArgumentException()
        {
            var largo501 = new string('y', Max + 1);

            TestDelegate act = () => _ = Descripcion.From(largo501);

            Assert.That(act, Throws.TypeOf<ArgumentException>()
                .With.Property("ParamName").EqualTo("texto"));
        }

        [Test]
        public void TryFrom_Ok_CuandoValido()
        {
            var ok = Descripcion.TryFrom("  Características:\n- Color verde  ", out var d);

            Assert.That(ok, Is.True);
            Assert.That(d, Is.Not.Null);
            Assert.That(d!.Texto, Is.EqualTo("Características:\n- Color verde"));
        }

        [Test]
        public void TryFrom_False_CuandoExcede500()
        {
            var largo501 = new string('z', Max + 1);

            var ok = Descripcion.TryFrom(largo501, out var d);

            Assert.That(ok, Is.False);
            Assert.That(d, Is.Null);
        }

        [Test]
        public void Igualdad_PorValor_ConRecord()
        {
            var a = Descripcion.From("  Hola mundo  ");
            var b = Descripcion.From("Hola mundo");

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.Texto, Is.EqualTo(b.Texto));
        }

        [Test]
        public void Desiguales_CuandoTextoDiferente()
        {
            var a = Descripcion.From("A");
            var b = Descripcion.From("B");

            Assert.That(a, Is.Not.EqualTo(b));
        }
    }
}

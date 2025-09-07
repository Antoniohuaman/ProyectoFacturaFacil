using System;
using NUnit.Framework;
using CatalogoArticulosBC.Domain.ValueObjects;

namespace CatalogoArticulosBC.Domain.Tests.ValueObjects
{
    [TestFixture]
    public class NombreProductoTests
    {
        // Cambia este valor a 200 si decides mantener tu límite actual.
        private const int Max = 250;

        [Test]
        public void Ctor_Valido_AplicaTrim_YAsignaValor()
        {
            var np = new NombreProducto("   Mochila escolar / Verde - Niños   ");

            Assert.That(np.Valor, Is.EqualTo("Mochila escolar / Verde - Niños"));
            Assert.That(np.ToString(), Is.EqualTo("Mochila escolar / Verde - Niños"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\t\r\n")]
        public void Ctor_NullOVacio_LanzaArgumentException(string? input)
        {
            TestDelegate act = () => _ = new NombreProducto(input!);

            Assert.That(act, Throws.TypeOf<ArgumentException>()
                .With.Property("ParamName").EqualTo("valor"));
        }

        [Test]
        public void Ctor_LongitudExactaMax_Acepta()
        {
            var texto = new string('A', Max);
            var np = new NombreProducto(texto);

            Assert.That(np.Valor.Length, Is.EqualTo(Max));
        }

        [Test]
        public void Ctor_LongitudMayorQueMax_LanzaArgumentException()
        {
            var texto = new string('B', Max + 1);

            TestDelegate act = () => _ = new NombreProducto(texto);

            Assert.That(act, Throws.TypeOf<ArgumentException>()
                .With.Property("ParamName").EqualTo("valor"));
        }

        [Test]
        public void Ctor_PermiteSoloCaracteresDefinidos_IncluyeAcentosYNumeros()
        {
            // Letras con acentos y ñ, dígitos, espacios, slash, guion y guion_bajo.
            var texto = "Lápiz_2B - Niño/Escolar 2025";
            var np = new NombreProducto(texto);

            Assert.That(np.Valor, Is.EqualTo(texto));
        }

        [TestCase("ACME, Inc")]     // coma
        [TestCase("Camisa: Verde")] // dos puntos
        [TestCase("Taza (cerámica)")] // paréntesis
        [TestCase("Funda.#1")]      // punto y numeral
        [TestCase("USB+Cable")]     // más
        [TestCase("Café@Home")]     // arroba
        public void Ctor_CaracterNoPermitido_LanzaArgumentException(string texto)
        {
            // Con el conjunto actual, estos deben fallar.
            TestDelegate act = () => _ = new NombreProducto(texto);

            Assert.That(act, Throws.TypeOf<ArgumentException>()
                .With.Property("ParamName").EqualTo("valor"));
        }

        [Test]
        public void Equals_MismoTextoMismoTrim_True_YHashIgual()
        {
            var a = new NombreProducto("  Mochila Verde_Infantil  ");
            var b = new NombreProducto("Mochila Verde_Infantil");

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void Equals_CaseSensitive()
        {
            var lower = new NombreProducto("mochila");
            var upper = new NombreProducto("Mochila");

            Assert.That(lower.Equals(upper), Is.False);
            Assert.That(lower, Is.Not.EqualTo(upper));
        }

        [Test]
        public void Equals_ContraNull_False()
        {
            var a = new NombreProducto("Producto");
            Assert.That(a.Equals(null), Is.False);
        }

        [Test]
        public void SoloTrim_NoColapsaEspaciosInternos()
        {
            var a = new NombreProducto("Prod   X  2025");
            Assert.That(a.Valor, Is.EqualTo("Prod   X  2025")); // conserva espacios internos
        }
    }
}

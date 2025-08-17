using System;
using System.Collections.Generic;
using NUnit.Framework;
using SharedKernel.ValueObjects;

namespace SharedKernel.Tests.UnitTests.ValueObjects
{
    [TestFixture]
    public class UnidadDeMedidaTests
    {
        // ---------------- CASOS FELICES ----------------

        [TestCase("NIU", "NIU")]
        [TestCase("kgm", "KGM")]
        [TestCase("c62", "C62")]
        [TestCase("CAJA", "CAJA")]
        [TestCase("abc-123", "ABC-123")]
        [TestCase("A_B", "A_B")]
        public void From_NormalizaAMayusculas_Y_ConservaFormatoValido(string input, string esperado)
        {
            var um = UnidadDeMedida.From(input);
            Assert.That(um.Codigo, Is.EqualTo(esperado));
            Assert.That(um.ToString(), Is.EqualTo(esperado)); // ToString = Codigo
        }

        [Test]
        public void Atajos_Comunes_Son_Equivalentes_A_From()
        {
            Assert.That(UnidadDeMedida.NIU, Is.EqualTo(UnidadDeMedida.From("NIU")));
            Assert.That(UnidadDeMedida.KGM, Is.EqualTo(UnidadDeMedida.From("KGM")));
            Assert.That(UnidadDeMedida.LTR, Is.EqualTo(UnidadDeMedida.From("LTR")));
            Assert.That(UnidadDeMedida.MTR, Is.EqualTo(UnidadDeMedida.From("MTR")));
            Assert.That(UnidadDeMedida.ZZ,  Is.EqualTo(UnidadDeMedida.From("ZZ")));
        }

        [Test]
        public void IgualdadPorValor_Funciona_EnColecciones()
        {
            var a = UnidadDeMedida.From("NIU");
            var b = UnidadDeMedida.From("niu"); // normaliza
            var set = new HashSet<UnidadDeMedida> { a, b };

            Assert.That(a, Is.EqualTo(b));
            Assert.That(set.Count, Is.EqualTo(1)); // mismos valores -> un solo elemento
        }

        [Test]
        public void Conversiones_ImplicitaYExplicita_Funcionan()
        {
            // explícita desde string
            UnidadDeMedida um = (UnidadDeMedida)"kgm";
            Assert.That(um.Codigo, Is.EqualTo("KGM"));

            // implícita a string
            string s = um;
            Assert.That(s, Is.EqualTo("KGM"));
        }

        // ---------------- BORDES ----------------

        [Test]
        public void From_Acepta_LongitudMinimaYMaxima()
        {
            var min = UnidadDeMedida.From("A");                // 1 char
            var max = UnidadDeMedida.From(new string('A', 15)); // 15 chars
            Assert.Multiple(() =>
            {
                Assert.That(min.Codigo, Is.EqualTo("A"));
                Assert.That(max.Codigo, Is.EqualTo(new string('A', 15)));
            });
        }

        [TestCase("A B")]     // espacio
        [TestCase("A/B")]     // slash
        [TestCase("A.B")]     // punto
        [TestCase("A*B")]     // asterisco
        [TestCase("A,B")]     // coma
        [TestCase("A@B")]     // arroba
        [TestCase("AáB")]     // acento (no ASCII A-Z)
        public void From_LanzaArgumentException_SiTieneCaracteresInvalidos(string invalido)
        {
            Assert.Throws<ArgumentException>(() => UnidadDeMedida.From(invalido));
        }

        [Test]
        public void From_LanzaArgumentOutOfRange_SiExcedeLongitudMaxima()
        {
            var s = new string('A', 16);
            Assert.Throws<ArgumentOutOfRangeException>(() => UnidadDeMedida.From(s));
        }

        [Test]
        public void From_LanzaArgumentException_SiEsNullOVacioOBlanco()
        {
            Assert.Throws<ArgumentException>(() => UnidadDeMedida.From((string)null!));
            Assert.Throws<ArgumentException>(() => UnidadDeMedida.From(string.Empty));
            Assert.Throws<ArgumentException>(() => UnidadDeMedida.From("   "));
        }

        // ---------------- TRYFROM ----------------

        [Test]
        public void TryFrom_DevuelveTrue_ConValorNormalizado()
        {
            var ok = UnidadDeMedida.TryFrom("kgm", out var um);
            Assert.Multiple(() =>
            {
                Assert.That(ok, Is.True);
                Assert.That(um, Is.Not.Null);
                Assert.That(um!.Codigo, Is.EqualTo("KGM"));
            });
        }

        [Test]
        public void TryFrom_DevuelveFalse_YNull_SiEsInvalido()
        {
            var ok = UnidadDeMedida.TryFrom("A B", out var um);
            Assert.Multiple(() =>
            {
                Assert.That(ok, Is.False);
                Assert.That(um, Is.Null);
            });
        }

        // ---------------- STRICT / SUNAT ----------------

        [Test]
        public void FromStrict_UsaWhitelist_ParaRestringirCodigos()
        {
            var sunat = new HashSet<string>(StringComparer.Ordinal) { "NIU", "KGM", "C62" };

            var permitido = UnidadDeMedida.FromStrict("c62", sunat.Contains);
            Assert.That(permitido.Codigo, Is.EqualTo("C62"));

            Assert.Throws<ArgumentException>(() => UnidadDeMedida.FromStrict("ZZZ", sunat.Contains));
        }
    }
}

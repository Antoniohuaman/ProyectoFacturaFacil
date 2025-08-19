using System;
using NUnit.Framework;
using CatalogoArticulosBC.Domain.ValueObjects;

namespace CatalogoArticulosBC.Tests.UnitTests.ValueObjects
{
    [TestFixture]
    public class ModeloTests
    {
        // ---------- Creación básica & normalización de espacios ----------

        [Test]
        public void From_NormalizaEspacios_Y_RespetaCasingEnValue()
        {
            var m = Modelo.From("  iPhone   15 Pro  ");
            Assert.Multiple(() =>
            {
                Assert.That(m.Value,      Is.EqualTo("iPhone 15 Pro"));  // colapsa y trim
                Assert.That(m.Normalized, Is.EqualTo("IPHONE 15 PRO"));  // mayúsculas invariantes
                Assert.That(m.ToString(), Is.EqualTo("iPhone 15 Pro"));  // ToString = Value
            });
        }

        [Test]
        public void From_Trim_LeadingTrailing()
        {
            var m = Modelo.From("   XPS 13 9320   ");
            Assert.That(m.Value, Is.EqualTo("XPS 13 9320"));
        }

        // ---------- Caracteres permitidos (regex) ----------

        [TestCase("XPS 13 9320")]
        [TestCase("CÁMARA PRO")]           // acentos
        [TestCase("VEGA-64")]
        [TestCase("Serie_AZ_01")]
        [TestCase("GO/PRO 11")]
        [TestCase("RX-7900 + BRACKET")]
        [TestCase("ThinkPad (Gen 2)")]
        [TestCase("MODELO #A1")]
        [TestCase("A.B.C-123")]
        public void From_AceptaCaracteresPermitidos(string input)
        {
            Assert.DoesNotThrow(() => Modelo.From(input));
        }

        [TestCase("@Modelo")]
        [TestCase("Mod%elo")]
        [TestCase("Mod$elo")]
        [TestCase("*Modelo")]
        [TestCase("Mod?elo")]
        public void From_RechazaCaracteresNoPermitidos(string input)
        {
            var ex = Assert.Throws<ArgumentException>(() => Modelo.From(input));
            Assert.That(ex!.Message, Does.Contain("solo admite letras, números, espacios y . - _ / + ( ) #"));
        }

        // ---------- Primer carácter debe ser alfanumérico ----------

        [Test]
        public void From_PrimerCaracterNoAlfanumerico_Rechaza()
        {
            var ex = Assert.Throws<ArgumentException>(() => Modelo.From("-XPS 13"));
            Assert.That(ex!.Message, Does.Contain("solo admite letras, números, espacios y . - _ / + ( ) #"));
        }

        // ---------- Límite de longitud ----------

        [Test]
        public void From_LongitudMaxima_60_Acepta()
        {
            var s60 = new string('A', 60);
            var m = Modelo.From(s60);
            Assert.That(m.Value.Length, Is.EqualTo(60));
        }

        [Test]
        public void From_LongitudMayorA60_Rechaza()
        {
            var s61 = new string('A', 61);
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Modelo.From(s61));
            Assert.That(ex!.Message, Does.Contain("no puede superar 60 caracteres"));
        }

        // ---------- Nulos / Vacíos ----------

        [TestCase("")]
        [TestCase(" ")]
        [TestCase("   ")]
        public void From_VacioOBlancos_Rechaza(string text)
        {
            Assert.Throws<ArgumentException>(() => Modelo.From(text));
        }

        [Test]
        public void TryFrom_Null_O_Invalido_NoLanza_YDevuelveFalse()
        {
            Assert.That(Modelo.TryFrom(null, out var n1), Is.False);
            Assert.That(n1, Is.Null);

            Assert.That(Modelo.TryFrom("@Modelo", out var n2), Is.False);
            Assert.That(n2, Is.Null);
        }

        [Test]
        public void TryFrom_Valido_TrueYObjeto()
        {
            var ok = Modelo.TryFrom("XPS 13 9320", out var m);
            Assert.Multiple(() =>
            {
                Assert.That(ok, Is.True);
                Assert.That(m,  Is.Not.Null);
                Assert.That(m!.Value, Is.EqualTo("XPS 13 9320"));
            });
        }

        // ---------- Igualdad por valor (case-insensitive + espacios normalizados) ----------

        [Test]
        public void Igualdad_CaseInsensitive_Y_EspaciosColapsados()
        {
            var a = Modelo.From(" XPS   13 9320 ");
            var b = Modelo.From("xps 13 9320");
            var c = Modelo.From("XPS 15 9520");

            Assert.Multiple(() =>
            {
                Assert.That(a, Is.EqualTo(b));
                Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
                Assert.That(a, Is.Not.EqualTo(c));
            });
        }

        // ---------- Conversiones & ToString ----------

        [Test]
        public void Implicit_ToString_DevuelveValue()
        {
            var m = Modelo.From("iPhone 15 Pro");
            string s = m; // conversión implícita
            Assert.That(s, Is.EqualTo("iPhone 15 Pro"));
        }

        [Test]
        public void Explicit_FromString_CreaVO()
        {
            Modelo m = (Modelo)"VEGA 56";
            Assert.That(m.Value, Is.EqualTo("VEGA 56"));
        }

        [Test]
        public void ToString_IgualAValue()
        {
            var m = Modelo.From("ThinkPad X1 Carbon");
            Assert.That(m.ToString(), Is.EqualTo("ThinkPad X1 Carbon"));
        }
    }
}

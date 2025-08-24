using System;
using NUnit.Framework;
using CatalogoArticulosBC.Domain.ValueObjects;

namespace CatalogoArticulosBC.Domain.Tests.ValueObjects
{
    [TestFixture]
    public class ModeloTests
    {
        // -------------------- Creación / validaciones básicas --------------------

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\t\r\n")]
        public void From_NullOVacio_LanzaArgumentException(string? input)
        {
            TestDelegate act = () => _ = Modelo.From(input!);

            Assert.That(act, Throws.TypeOf<ArgumentException>()
                .With.Property("ParamName").EqualTo("text"));
        }

        [Test]
        public void From_Valido_NormalizaEspaciosYMayusculas_SeteaValueYNormalized()
        {
            var m = Modelo.From("  XPS   13   9320  ");

            Assert.That(m.Value, Is.EqualTo("XPS 13 9320"));
            Assert.That(m.Normalized, Is.EqualTo("XPS 13 9320")); // ya está en mayúsculas
            Assert.That(m.ToString(), Is.EqualTo("XPS 13 9320"));
        }

        [Test]
        public void From_ColapsaTabsYSaltosDeLinea()
        {
            var m = Modelo.From("XPS\t13\n9320");
            Assert.That(m.Value, Is.EqualTo("XPS 13 9320"));
            Assert.That(m.Normalized, Is.EqualTo("XPS 13 9320"));
        }

        [Test]
        public void From_LongitudExactaMax_Acepta()
        {
            var s = new string('A', Modelo.MaxLength);

            var m = Modelo.From(s);

            Assert.That(m.Value.Length, Is.EqualTo(Modelo.MaxLength));
            Assert.That(m.Normalized, Is.EqualTo(s)); // 'A' ya está en mayúscula
        }

        [Test]
        public void From_LongitudMayorQueMax_LanzaOutOfRange()
        {
            var s = new string('A', Modelo.MaxLength + 1);

            TestDelegate act = () => _ = Modelo.From(s);

            Assert.That(act, Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("text"));
        }

        // -------------------- Caracteres permitidos / inválidos --------------------

        [Test]
        public void From_PermiteCaracteresDefinidos_AcentosYSimbolosPermitidos()
        {
            var texto = "VÉGA PRO_2/64 (Edición) #G+ 1.0";

            var m = Modelo.From(texto);

            Assert.That(m.Value, Is.EqualTo("VÉGA PRO_2/64 (Edición) #G+ 1.0"));
            Assert.That(m.Normalized, Is.EqualTo("VÉGA PRO_2/64 (EDICIÓN) #G+ 1.0"));
        }

        [TestCase("ACME, Inc")]     // coma no permitida
        [TestCase("ACME:VEGA")]     // dos puntos no permitidos
        [TestCase("XPS@13")]        // arroba no permitida
        [TestCase("XPS|13")]        // pipe no permitido
        [TestCase("ACME;VEGA")]     // punto y coma no permitido
        public void From_CaracterNoPermitido_LanzaArgumentException(string texto)
        {
            TestDelegate act = () => _ = Modelo.From(texto);

            Assert.That(act, Throws.TypeOf<ArgumentException>()
                .With.Property("ParamName").EqualTo("text"));
        }

        [TestCase("-MODEL 1")] // primer char debe ser letra/dígito
        [TestCase("#HASH")]
        [TestCase("(XPS 13)")]
        [TestCase("+PLUS")]
        public void From_PrimerCaracterInvalido_LanzaArgumentException(string texto)
        {
            TestDelegate act = () => _ = Modelo.From(texto);

            Assert.That(act, Throws.TypeOf<ArgumentException>()
                .With.Property("ParamName").EqualTo("text"));
        }

        // -------------------- Igualdad (case-insensitive, acento-sensible) --------------------

        [Test]
        public void Igualdad_IgnoraMayusculasYEspaciosExtra_PeroMantieneAcentos()
        {
            // MISMOS acentos para que sean iguales; ignora case y espacios
            var a = Modelo.From("véga   pro_2/64 (edición) #g+");
            var b = Modelo.From("  VÉGA PRO_2/64  (EDICIÓN)   #G+ ");

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            Assert.That(a == b, Is.True);
            Assert.That(a != b, Is.False);
        }

        [Test]
        public void Igualdad_RespetaAcentos_NoIgualCuandoDiferenEnTildes()
        {
            var conAcento = Modelo.From("CANCIÓN125");
            var sinAcento = Modelo.From("CANCION125");

            Assert.That(conAcento, Is.Not.EqualTo(sinAcento));
            Assert.That(conAcento.Equals(sinAcento), Is.False);
            Assert.That(conAcento == sinAcento, Is.False);
            Assert.That(conAcento != sinAcento, Is.True);
        }

        [Test]
        public void Equals_ContraNull_EsFalso()
        {
            var m = Modelo.From("XPS 13");

            Assert.That(m.Equals(null), Is.False);
        }

        [Test]
        public void Operadores_ConNulls_SeComportanCorrectamente()
        {
            Modelo? a = null;
            Modelo? b = null;
            var c = Modelo.From("VEGA");

            Assert.That(a == b, Is.True);      // ambos null
            Assert.That(a == c, Is.False);     // null vs objeto
            Assert.That(c != a, Is.True);
        }

        // -------------------- TryFrom y conversiones --------------------

        [Test]
        public void TryFrom_Valido_TrueYObjetoNoNulo()
        {
            var ok = Modelo.TryFrom("  iPhone 15   Pro  ", out var m);

            Assert.That(ok, Is.True);
            Assert.That(m, Is.Not.Null);
            Assert.That(m!.Value, Is.EqualTo("iPhone 15 Pro"));
            Assert.That(m.Normalized, Is.EqualTo("IPHONE 15 PRO"));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("@@")] // inválido por caracteres
        public void TryFrom_Invalido_FalseYNull(string input)
        {
            var ok = Modelo.TryFrom(input, out var m);

            Assert.That(ok, Is.False);
            Assert.That(m, Is.Null);
        }

        [Test]
        public void Conversion_ImplicitaAString_DevuelveValue()
        {
            var m = Modelo.From("XPS 13");
            string s = m; // implícita

            Assert.That(s, Is.EqualTo("XPS 13"));
        }

        [Test]
        public void Conversion_ExplicitaDesdeString_UsaFrom()
        {
            var m = (Modelo)"  vega  pro_2/64 (edición) #g+ ";

            Assert.That(m.Value, Is.EqualTo("vega pro_2/64 (edición) #g+"));
            Assert.That(m.Normalized, Is.EqualTo("VEGA PRO_2/64 (EDICIÓN) #G+"));
        }

        // -------------------- ToString --------------------

        [Test]
        public void ToString_DevuelveValue()
        {
            var m = Modelo.From("VEGA");
            Assert.That(m.ToString(), Is.EqualTo("VEGA"));
        }
    }
}

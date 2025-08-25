using System;
using NUnit.Framework;
using ConfiguracionSistemaBC.Domain.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.UnitTests.ValueObjects
{
    [TestFixture]
    public class CorrelativoTests
    {
        [Test]
        public void From_Int_Valido_AsignacionCorrecta()
        {
            var c = Correlativo.From(1);
            Assert.That(c.Valor, Is.EqualTo(1));
            Assert.That(c.FormatoSunat8, Is.EqualTo("00000001"));
            Assert.That(c.EsMaximo, Is.False);
        }

        [Test]
        public void From_Int_FueraDeRango_LanzaArgumentOutOfRange()
        {
            Assert.That(() => Correlativo.From(0), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Correlativo.From(100_000_000), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TryFrom_Int_ComportamientoEsperado()
        {
            Assert.That(Correlativo.TryFrom(1, out var ok), Is.True);
            Assert.That(ok!.Valor, Is.EqualTo(1));

            Assert.That(Correlativo.TryFrom(0, out _), Is.False);
            Assert.That(Correlativo.TryFrom(100_000_000, out _), Is.False);
        }

        [Test]
        public void FromString_AceptaCerosIzquierda_Y_Normaliza()
        {
            var c1 = Correlativo.FromString("00000001");
            Assert.That(c1.Valor, Is.EqualTo(1));
            Assert.That(c1.FormatoSunat8, Is.EqualTo("00000001"));

            var c2 = Correlativo.FromString("9135");
            Assert.That(c2.Valor, Is.EqualTo(9135));
            Assert.That(c2.FormatoSunat8, Is.EqualTo("00009135"));
        }

        [Test]
        public void FromString_Invalido_LanzaExcepcionAdecuada()
        {
            // no dígitos
            Assert.That(() => Correlativo.FromString(""), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => Correlativo.FromString("  "), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => Correlativo.FromString("12A3"), Throws.TypeOf<ArgumentOutOfRangeException>());

            // más de 8 dígitos
            Assert.That(() => Correlativo.FromString("123456789"), Throws.TypeOf<ArgumentOutOfRangeException>());

            // numérico pero fuera de rango (0)
            Assert.That(() => Correlativo.FromString("00000000"), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TryFromString_ComportamientoEsperado()
        {
            Assert.That(Correlativo.TryFromString("00000001", out var c1), Is.True);
            Assert.That(c1!.Valor, Is.EqualTo(1));

            Assert.That(Correlativo.TryFromString("12345678", out var c2), Is.True);
            Assert.That(c2!.Valor, Is.EqualTo(12_345_678));

            Assert.That(Correlativo.TryFromString(null, out _), Is.False);
            Assert.That(Correlativo.TryFromString("", out _), Is.False);
            Assert.That(Correlativo.TryFromString("12A3", out _), Is.False);
            Assert.That(Correlativo.TryFromString("123456789", out _), Is.False);
            Assert.That(Correlativo.TryFromString("00000000", out _), Is.False);
        }

        [Test]
        public void Siguiente_IncrementaInmutablemente_Y_FallaEnMaximo()
        {
            var c = Correlativo.From(1350);
            var next = c.Siguiente();

            // Inmutabilidad
            Assert.That(c.Valor, Is.EqualTo(1350));
            Assert.That(next.Valor, Is.EqualTo(1351));
            Assert.That(next.FormatoSunat8, Is.EqualTo("00001351"));

            // Overflow en máximo
            var max = Correlativo.From(Correlativo.Max);
            Assert.That(max.EsMaximo, Is.True);
            Assert.That(() => max.Siguiente(), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void IgualdadPorValor_OperadoresYHashCode()
        {
            var a = Correlativo.FromString("00000001");
            var b = Correlativo.From(1);
            var c = Correlativo.From(2);

            Assert.That(a.Equals(b), Is.True);
            Assert.That(a == b, Is.True);
            Assert.That(a != b, Is.False);
            Assert.That(a.Equals(c), Is.False);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void Conversiones_Int_Y_String()
        {
            var c = Correlativo.From(42);

            // implícito a int
            int v = c;
            Assert.That(v, Is.EqualTo(42));

            // explícito desde int
            var c2 = (Correlativo)7;
            Assert.That(c2.Valor, Is.EqualTo(7));
            Assert.That(c2.FormatoSunat8, Is.EqualTo("00000007"));

            // explícito desde string
            var c3 = (Correlativo)"00000100";
            Assert.That(c3.Valor, Is.EqualTo(100));
            Assert.That(c3.FormatoSunat8, Is.EqualTo("00000100"));

            // ToString
            Assert.That(c.ToString(), Is.EqualTo("42"));
        }
    }
}
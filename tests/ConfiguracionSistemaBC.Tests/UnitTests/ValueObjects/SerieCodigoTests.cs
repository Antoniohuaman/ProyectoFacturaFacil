using System;
using NUnit.Framework;
using ConfiguracionSistemaBC.Domain.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.UnitTests.ValueObjects
{
    [TestFixture]
    public class SerieCodigoTests
    {
        [Test]
        public void From_FormatoBasicoValido_NormalizaMayusculasYTrim()
        {
            var s1 = SerieCodigo.From("F001");
            Assert.That(s1.Codigo, Is.EqualTo("F001"));
            Assert.That(s1.Prefijo, Is.EqualTo('F'));

            var s2 = SerieCodigo.From("  b123 ");
            Assert.That(s2.Codigo, Is.EqualTo("B123"));
            Assert.That(s2.Prefijo, Is.EqualTo('B'));
        }

        [Test]
        public void From_FormatoBasicoInvalido_LanzaArgumentOutOfRange()
        {
            // null
            Assert.That(() => SerieCodigo.From(null!), Throws.TypeOf<ArgumentNullException>());

            // longitud distinta de 4
            Assert.That(() => SerieCodigo.From("F01"),  Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => SerieCodigo.From("F0001"),Throws.TypeOf<ArgumentOutOfRangeException>());

            // primer caracter no es letra
            Assert.That(() => SerieCodigo.From("1001"), Throws.TypeOf<ArgumentOutOfRangeException>());

            // últimos no son todos dígitos
            Assert.That(() => SerieCodigo.From("FA01"), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => SerieCodigo.From("F0A1"), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => SerieCodigo.From("F01A"), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void EsFormatoBasicoValido_CasosPositivosYNegativos()
        {
            Assert.That(SerieCodigo.EsFormatoBasicoValido("F001"), Is.True);
            Assert.That(SerieCodigo.EsFormatoBasicoValido("B123"), Is.True);
            Assert.That(SerieCodigo.EsFormatoBasicoValido("Z999"), Is.True);

            Assert.That(SerieCodigo.EsFormatoBasicoValido("f001"), Is.False); // la función espera ya normalizado
            Assert.That(SerieCodigo.EsFormatoBasicoValido("AA01"), Is.False);
            Assert.That(SerieCodigo.EsFormatoBasicoValido("A01"),  Is.False);
            Assert.That(SerieCodigo.EsFormatoBasicoValido("A0A1"), Is.False);
            Assert.That(SerieCodigo.EsFormatoBasicoValido("1001"), Is.False);
        }

        [Test]
        public void ForTipo_Factura_RequierePrefijoF()
        {
            var ok = SerieCodigo.ForTipo("f001", TipoComprobanteCodigo.Factura);
            Assert.That(ok.Codigo, Is.EqualTo("F001"));
            Assert.That(ok.EsValidaPara(TipoComprobanteCodigo.Factura), Is.True);

            var ex = Assert.Throws<ArgumentException>(() => SerieCodigo.ForTipo("B001", TipoComprobanteCodigo.Factura));
            Assert.That(ex!.Message, Does.Contain("Debe iniciar con 'F'"));
        }

        [Test]
        public void ForTipo_Boleta_RequierePrefijoB()
        {
            var ok = SerieCodigo.ForTipo("b123", TipoComprobanteCodigo.Boleta);
            Assert.That(ok.Codigo, Is.EqualTo("B123"));
            Assert.That(ok.EsValidaPara(TipoComprobanteCodigo.Boleta), Is.True);

            var ex = Assert.Throws<ArgumentException>(() => SerieCodigo.ForTipo("F001", TipoComprobanteCodigo.Boleta));
            Assert.That(ex!.Message, Does.Contain("Debe iniciar con 'B'"));
        }

        [Test]
        public void TryFrom_Y_TryForTipo_ComportamientoEsperado()
        {
            Assert.That(SerieCodigo.TryFrom("f001", out var s1), Is.True);
            Assert.That(s1!.Codigo, Is.EqualTo("F001"));

            Assert.That(SerieCodigo.TryFrom("FA01", out _), Is.False); // formato inválido

            Assert.That(SerieCodigo.TryForTipo("f001", TipoComprobanteCodigo.Factura, out var s2), Is.True);
            Assert.That(s2!.Codigo, Is.EqualTo("F001"));

            Assert.That(SerieCodigo.TryForTipo("F001", TipoComprobanteCodigo.Boleta, out _), Is.False); // prefijo no coincide
        }

        [Test]
        public void EsValidaPara_VerificaPrefijoSegunTipo()
        {
            var f = SerieCodigo.From("F001");
            var b = SerieCodigo.From("B001");

            Assert.That(f.EsValidaPara(TipoComprobanteCodigo.Factura), Is.True);
            Assert.That(f.EsValidaPara(TipoComprobanteCodigo.Boleta),  Is.False);

            Assert.That(b.EsValidaPara(TipoComprobanteCodigo.Boleta),  Is.True);
            Assert.That(b.EsValidaPara(TipoComprobanteCodigo.Factura), Is.False);
        }

        [Test]
        public void IgualdadPorValor_OperadoresYHashCode()
        {
            var a = SerieCodigo.From("F001");
            var b = SerieCodigo.From("f001");
            var c = SerieCodigo.From("F002");

            Assert.That(a.Equals(b), Is.True);
            Assert.That(a == b, Is.True);
            Assert.That(a != b, Is.False);
            Assert.That(a.Equals(c), Is.False);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void Conversiones_ToString_ImplícitoYExplícito()
        {
            var s = SerieCodigo.From("b123");

            Assert.That(s.ToString(), Is.EqualTo("B123"));

            string plain = s; // implícito
            Assert.That(plain, Is.EqualTo("B123"));

            var s2 = (SerieCodigo)" f001 "; // explícito (valida y normaliza)
            Assert.That(s2.Codigo, Is.EqualTo("F001"));
        }
    }
}
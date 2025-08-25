using System;
using NUnit.Framework;
using ConfiguracionSistemaBC.Domain.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.UnitTests.ValueObjects
{
    [TestFixture]
    public class RucTests
    {
        // Casos válidos (tomados de tus XML y uno generado con prefijo 10)
        private const string RucEmpresaJuridica1 = "20600552849";
        private const string RucEmpresaJuridica2 = "20606272741";
        private const string RucEmpresaJuridica3 = "20120571487";
        private const string RucEmpresaJuridica4 = "20601131952";
        // Persona natural con negocio (prefijo 10) válido:
        private const string RucPersonaNatural   = "10788811816";

        [Test]
        public void FromString_Valido_NormalizaYValidaDV()
        {
            // Sin separadores
            var r1 = Ruc.FromString(RucEmpresaJuridica1);
            Assert.That(r1.Numero, Is.EqualTo(RucEmpresaJuridica1));
            Assert.That(r1.EsPersonaJuridica, Is.True);
            Assert.That(r1.EsPersonaNaturalConNegocio, Is.False);
            Assert.That(r1.Prefijo, Is.EqualTo("20"));
            Assert.That(r1.Base10, Is.EqualTo(RucEmpresaJuridica1.Substring(0, 10)));
            Assert.That(r1.DigitoVerificador, Is.EqualTo(RucEmpresaJuridica1[10] - '0'));

            // Con separadores y espacios (debe normalizar a los 11 dígitos)
            var r2 = Ruc.FromString("2060-055-2849");
            Assert.That(r2.Numero, Is.EqualTo(RucEmpresaJuridica1));
            Assert.That(r2, Is.EqualTo(r1));

            // Prefijo 10 (PN con negocio) válido
            var r3 = Ruc.FromString(RucPersonaNatural);
            Assert.That(r3.EsPersonaNaturalConNegocio, Is.True);
            Assert.That(r3.EsPersonaJuridica, Is.False);
            Assert.That(r3.Prefijo, Is.EqualTo("10"));
        }

        [Test]
        public void FromString_LongitudInvalida_LanzaArgumentOutOfRange()
        {
            Assert.That(() => Ruc.FromString("2012345678"),  // 10 dígitos
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Ruc.FromString("201234567890"), // 12 dígitos
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Ruc.FromString(""),             // vacío
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Ruc.FromString(null!),          // null
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void FromString_PrefijoNoPermitido_LanzaArgumentOutOfRange()
        {
            // 30xxxxx... con DV cualquiera (longitud correcta pero prefijo inválido)
            Assert.That(() => Ruc.FromString("30600552849"),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void FromString_DigitoVerificadorIncorrecto_LanzaArgumentException()
        {
            // Cambiamos el último dígito a uno incorrecto
            var invalido = "20600552848"; // debería terminar en ...9 para ser válido
            Assert.That(() => Ruc.FromString(invalido),
                Throws.TypeOf<ArgumentException>().With.Message.Contains("verificador"));
        }

        [Test]
        public void TryFrom_DevuelveTrueParaValidos_YFalseParaInvalidos()
        {
            Assert.That(Ruc.TryFrom(RucEmpresaJuridica2, out var ok1), Is.True);
            Assert.That(ok1!.Numero, Is.EqualTo(RucEmpresaJuridica2));

            // El RUC "2060 623 2741" (con espacios) no es válido según el Value Object, así que debe devolver false
            Assert.That(Ruc.TryFrom("2060 623 2741", out var ok2), Is.False);

            Assert.That(Ruc.TryFrom("ABC", out _), Is.False); // sin 11 dígitos
            Assert.That(Ruc.TryFrom("30600552849", out _), Is.False); // prefijo inválido
            Assert.That(Ruc.TryFrom("20600552848", out _), Is.False); // DV incorrecto
            Assert.That(Ruc.TryFrom(null, out _), Is.False);
        }

        [Test]
        public void EsValido_AtajosDeValidacion()
        {
            Assert.That(Ruc.EsValido(RucEmpresaJuridica3), Is.True);
            Assert.That(Ruc.EsValido("2012-057-1487"), Is.True); // normaliza y valida
            Assert.That(Ruc.EsValido("20120571488"), Is.False);  // DV incorrecto
            Assert.That(Ruc.EsValido("30-120571487"), Is.False); // prefijo inválido
            Assert.That(Ruc.EsValido(""), Is.False);
        }

        [Test]
        public void IgualdadPorValor_OperadoresYHashCode()
        {
            var a = Ruc.FromString(RucEmpresaJuridica4);
            var b = Ruc.FromString("2060-113-1952"); // mismo RUC normalizado
            var c = Ruc.FromString(RucEmpresaJuridica1);

            Assert.That(a.Equals(b), Is.True);
            Assert.That(a == b, Is.True);
            Assert.That(a != b, Is.False);
            Assert.That(a.Equals(c), Is.False);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void Conversiones_ToString_ImplícitoYExplícito()
        {
            var r = Ruc.FromString(RucEmpresaJuridica1);
            Assert.That(r.ToString(), Is.EqualTo(RucEmpresaJuridica1));

            string s = r; // implícito a string
            Assert.That(s, Is.EqualTo(RucEmpresaJuridica1));

            var r2 = (Ruc)"2060 055 2849"; // explícito desde string (normaliza)
            Assert.That(r2.Numero, Is.EqualTo(RucEmpresaJuridica1));
        }
    }
}
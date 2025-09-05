using System;
using NUnit.Framework;
using ConfiguracionSistemaBC.Domain.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.ValueObjects
{
    [TestFixture]
    public class RucTests
    {
        // RUC válidos (calculados con el algoritmo del VO):
        // PN (10, 15, 17) y PJ (20)
        private const string RucValidoPN10 = "10141381933";
        private const string RucValidoPN15 = "15597441841";
        private const string RucValidoPN17 = "17798528050";
        private const string RucValidoPJ20 = "20342996940";

        // Versiones con DV alterado para probar error de DV:
        private const string RucInvalidoDv_PN10 = "10141381934";
        private const string RucInvalidoDv_PN15 = "15597441842";
        private const string RucInvalidoDv_PN17 = "17798528051";
        private const string RucInvalidoDv_PJ20 = "20342996941";

        [Test]
        [TestCase(RucValidoPN10, "10", true,  false, true)]  // PN10: Natural + ConNegocio
        [TestCase(RucValidoPN15, "15", true,  false, false)] // PN15: Natural
        [TestCase(RucValidoPN17, "17", true,  false, false)] // PN17: Natural
        [TestCase(RucValidoPJ20, "20", false, true,  false)] // PJ20: Jurídica
        public void FromString_Valido_DeberiaCrearRucConPropiedadesCorrectas(
            string rucText, string prefijoEsperado, bool esPN, bool esPJ, bool esPNConNegocio)
        {
            var ruc = Ruc.FromString(rucText);

            Assert.That(ruc, Is.Not.Null);
            Assert.That(ruc.Numero, Is.EqualTo(rucText));
            Assert.That(ruc.Canonizado, Is.EqualTo(rucText));
            Assert.That(ruc.Prefijo, Is.EqualTo(prefijoEsperado));
            Assert.That(ruc.EsPersonaNatural, Is.EqualTo(esPN));
            Assert.That(ruc.EsPersonaJuridica, Is.EqualTo(esPJ));
            Assert.That(ruc.EsPersonaNaturalConNegocio, Is.EqualTo(esPNConNegocio));

            Assert.That(ruc.Numero.Length, Is.EqualTo(11));
            Assert.That(ruc.Base10.Length, Is.EqualTo(10));
            Assert.That(ruc.Base10 + ruc.DigitoVerificador, Is.EqualTo(ruc.Numero));
        }

        [Test]
        public void FromString_NormalizaFormato_PermiteEspaciosYGuiones()
        {
            // Toma el válido y le agrega espacios/guiones — debe canonizar a los 11 dígitos
            var crudo = "20 342-99694 - 0";
            var ruc = Ruc.FromString(crudo);

            Assert.That(ruc.Numero, Is.EqualTo(RucValidoPJ20)); // canonizado
            Assert.That(ruc.Prefijo, Is.EqualTo("20"));
            Assert.That(ruc.EsPersonaJuridica, Is.True);
            Assert.That(ruc.EsPersonaNatural, Is.False);
        }

        [Test]
        public void FromString_LongitudDistintaA11_DeberiaLanzarExcepcion()
        {
            Assert.That(() => Ruc.FromString("2012345678"), // 10 dígitos
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void FromString_PrefijoInvalido_DeberiaLanzarExcepcion()
        {
            // 12xxxxxxxxx (prefijo no permitido). La longitud es 11 para que caiga por prefijo.
            Assert.That(() => Ruc.FromString("12123456789"),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        [TestCase(RucInvalidoDv_PN10)]
        [TestCase(RucInvalidoDv_PN15)]
        [TestCase(RucInvalidoDv_PN17)]
        [TestCase(RucInvalidoDv_PJ20)]
        public void FromString_DigitoVerificadorIncorrecto_DeberiaLanzarExcepcion(string rucConDvErrado)
        {
            Assert.That(() => Ruc.FromString(rucConDvErrado),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void TryFrom_Valido_DeberiaRetornarTrueYOutInstancia()
        {
            var ok = Ruc.TryFrom(RucValidoPN15, out var ruc);

            Assert.That(ok, Is.True);
            Assert.That(ruc, Is.Not.Null);
            Assert.That(ruc!.Numero, Is.EqualTo(RucValidoPN15));
        }

        [Test]
        public void TryFrom_Invalido_DeberiaRetornarFalseYOutNull()
        {
            var ok = Ruc.TryFrom("12123456789", out var ruc);

            Assert.That(ok, Is.False);
            Assert.That(ruc, Is.Null);
        }

        [Test]
        public void EsValido_DeberiaReflejarResultadoDeTryFrom()
        {
            Assert.That(Ruc.EsValido(RucValidoPN17), Is.True);
            Assert.That(Ruc.EsValido(RucInvalidoDv_PN17), Is.False);
            Assert.That(Ruc.EsValido(""), Is.False);
            Assert.That(Ruc.EsValido(null), Is.False);
        }

        [Test]
        public void IgualdadPorValor_MismoNumero_DeberiaSerIgual()
        {
            var a = Ruc.FromString(RucValidoPJ20);
            var b = Ruc.FromString(RucValidoPJ20);

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a == b, Is.True);
            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void Operadores_ToString_ImplicitExplicit_DeberianFuncionar()
        {
            var r = Ruc.FromString(RucValidoPJ20);

            string s = r; // implícito a string
            Assert.That(s, Is.EqualTo(RucValidoPJ20));
            Assert.That(r.ToString(), Is.EqualTo(RucValidoPJ20));

            var r2 = (Ruc)RucValidoPJ20; // explícito desde string
            Assert.That(r2, Is.EqualTo(r));
        }

        [Test]
        public void PropiedadesPrefijoBase10Dv_DeberianSerCoherentes()
        {
            var r = Ruc.FromString(RucValidoPN10);

            Assert.That(r.Prefijo, Is.EqualTo("10"));
            Assert.That(r.Base10, Is.EqualTo(RucValidoPN10.Substring(0, 10)));
            Assert.That(r.DigitoVerificador, Is.EqualTo(RucValidoPN10[10] - '0'));
        }
    }
}

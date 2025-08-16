using System;
using NUnit.Framework;
using SharedKernel.ValueObjects;

// 👇 añade estos alias para usar el SharedKernel
using MonedaSK = SharedKernel.ValueObjects.Moneda;
using DineroSK = SharedKernel.ValueObjects.Dinero;

namespace SharedKernel.Tests.ValueObjects
{
    [TestFixture]
    public class MonedaTests
    {
        // -----------------------
        // Fábricas conocidas (PEN / USD)
        // -----------------------

        [Test]
        public void PEN_Fabrica_DeberiaRetornarValoresEsperados()
        {
            var pen = Moneda.PEN();

            Assert.That(pen.Codigo, Is.EqualTo("PEN"));
            Assert.That(pen.Simbolo, Is.EqualTo("S/."));
            Assert.That(pen.Decimales, Is.EqualTo(2));
        }

        [Test]
        public void USD_Fabrica_DeberiaRetornarValoresEsperados()
        {
            var usd = Moneda.USD();

            Assert.That(usd.Codigo, Is.EqualTo("USD"));
            Assert.That(usd.Simbolo, Is.EqualTo("$"));
            Assert.That(usd.Decimales, Is.EqualTo(2));
        }

        // -----------------------
        // Create con defaults conocidos (diccionario, case-insensitive)
        // -----------------------

        [Test]
        public void Create_PEN_SinSimboloYDecimales_UsaDefaultsConocidos()
        {
            var m = Moneda.Create("pen"); // minúsculas a propósito

            Assert.That(m.Codigo, Is.EqualTo("PEN"));
            Assert.That(m.Simbolo, Is.EqualTo("S/."));   // toma del diccionario
            Assert.That(m.Decimales, Is.EqualTo(2));    // toma del diccionario
        }

        [Test]
        public void Create_USD_SinSimboloYDecimales_UsaDefaultsConocidos()
        {
            var m = Moneda.Create("usd");

            Assert.That(m.Codigo, Is.EqualTo("USD"));
            Assert.That(m.Simbolo, Is.EqualTo("$"));
            Assert.That(m.Decimales, Is.EqualTo(2));
        }

        [Test]
        public void Create_PEN_ConOverrides_IgnoraDefaultsYRespetaArgumentos()
        {
            var m = Moneda.Create("PEN", simbolo: "SOL", decimales: 3);

            Assert.That(m.Codigo, Is.EqualTo("PEN"));
            Assert.That(m.Simbolo, Is.EqualTo("SOL"));  // override
            Assert.That(m.Decimales, Is.EqualTo(3));    // override
        }

        // -----------------------
        // Create con código desconocido
        // -----------------------

        [Test]
        public void Create_CodigoDesconocido_SinSimbolo_UsaCodigoComoSimbolo_YDecimalesPorDefecto()
        {
            var m = Moneda.Create("eur"); // no está en defaults

            Assert.That(m.Codigo, Is.EqualTo("EUR"));
            Assert.That(m.Simbolo, Is.EqualTo("EUR"));  // fallback al código
            Assert.That(m.Decimales, Is.EqualTo(2));    // default del método
        }

        [Test]
        public void Create_CodigoDesconocido_ConSimboloYDecimales_RespetaArgumentos()
        {
            var m = Moneda.Create("clp", "$", decimales: 0);

            Assert.That(m.Codigo, Is.EqualTo("CLP"));
            Assert.That(m.Simbolo, Is.EqualTo("$"));
            Assert.That(m.Decimales, Is.EqualTo(0));
        }

        // -----------------------
        // Normalización y trimming
        // -----------------------

        [Test]
        public void Create_ConEspaciosYMinusculas_NormalizaAMayusculasYTrim()
        {
            var m = Moneda.Create("  pen  ", "  S/  ", 2);

            Assert.That(m.Codigo, Is.EqualTo("PEN"));
            Assert.That(m.Simbolo, Is.EqualTo("S/"));
            Assert.That(m.Decimales, Is.EqualTo(2));
        }

        // -----------------------
        // Igualdad por valor (record)
        // -----------------------

        [Test]
        public void Igualdad_PorValor_True_SiPropiedadesIguales()
        {
            var a = Moneda.Create("PEN");
            var b = Moneda.PEN();

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.Equals(b), Is.True);
        }

        [Test]
        public void Desigualdad_PorValor_True_SiPropiedadesDiferentes()
        {
            var a = Moneda.Create("PEN");
            var b = Moneda.Create("USD");

            Assert.That(a, Is.Not.EqualTo(b));
            Assert.That(a.Equals(b), Is.False);
        }

        // -----------------------
        // Validaciones y errores
        // -----------------------

        [Test]
        public void Create_CodigoNullOVacio_LanzaArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _ = Moneda.Create(""));
            Assert.Throws<ArgumentException>(() => _ = Moneda.Create("  "));
            Assert.Throws<ArgumentException>(() => _ = Moneda.Create(null!));
        }

        [Test]
        public void Create_CodigoLongitudDistintaDe3_LanzaArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _ = Moneda.Create("PE"));     // 2 letras
            Assert.Throws<ArgumentException>(() => _ = Moneda.Create("PENX"));   // 4 letras
        }

        [Test]
        public void Create_CodigoConCaracteresNoLetras_LanzaArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _ = Moneda.Create("P3N"));
            Assert.Throws<ArgumentException>(() => _ = Moneda.Create("12A"));
            Assert.Throws<ArgumentException>(() => _ = Moneda.Create("P_N"));
        }

        [Test]
        public void Create_DecimalesFueraDeRango_LanzaArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = Moneda.Create("PEN", "S/", decimales: 5)); // >4
        }

        [TestCase((byte)0)]
        [TestCase((byte)1)]
        [TestCase((byte)2)]
        [TestCase((byte)3)]
        [TestCase((byte)4)]
        public void Create_DecimalesPermitidos_0a4(byte dec)
        {
            var m = Moneda.Create("PEN", "S/", dec);
            Assert.That(m.Decimales, Is.EqualTo(dec));
        }

        // -----------------------
        // TryCreate
        // -----------------------

        [Test]
        public void TryCreate_Valido_UsaDefaults_SaleTrue()
        {
            var ok = Moneda.TryCreate("pen", out var m);

            Assert.That(ok, Is.True);
            Assert.That(m, Is.Not.Null);
            Assert.That(m!.Codigo, Is.EqualTo("PEN"));
            Assert.That(m.Simbolo, Is.EqualTo("S/."));
            Assert.That(m.Decimales, Is.EqualTo(2));
        }

        [Test]
        public void TryCreate_Valido_ConOverrides_SaleTrueYRespetaArgumentos()
        {
            var ok = Moneda.TryCreate("CLP", out var m, simbolo: "$", decimales: 0);

            Assert.That(ok, Is.True);
            Assert.That(m, Is.Not.Null);
            Assert.That(m!.Codigo, Is.EqualTo("CLP"));
            Assert.That(m.Simbolo, Is.EqualTo("$"));
            Assert.That(m.Decimales, Is.EqualTo(0));
        }

        [Test]
        public void TryCreate_CodigoInvalido_SaleFalseYMonedaNull()
        {
            var ok = Moneda.TryCreate("P3N", out var m);

            Assert.That(ok, Is.False);
            Assert.That(m, Is.Null);
        }

        [Test]
        public void TryCreate_DecimalesFueraDeRango_SaleFalse()
        {
            var ok = Moneda.TryCreate("PEN", out var m, simbolo: "S/", decimales: 5); // >4

            Assert.That(ok, Is.False);
            Assert.That(m, Is.Null);
        }
    }
}

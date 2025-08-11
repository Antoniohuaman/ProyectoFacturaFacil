using System;
using System.Collections.Generic;
using NUnit.Framework;
using ConfiguracionSistemaBC.Domain.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.UnitTests.ValueObjects
{
    [TestFixture]
    public class MonedaTests
    {
        [Test]
        public void Instancias_Soportadas_EstanDefinidasCorrectamente()
        {
            Assert.That(Moneda.PEN.Codigo, Is.EqualTo("PEN"));
            Assert.That(Moneda.PEN.Simbolo, Is.EqualTo("S/."));
            Assert.That(Moneda.PEN.Decimales, Is.EqualTo(2));
            Assert.That(Moneda.PEN.EsMonedaNacional, Is.True);
            Assert.That(Moneda.PEN.EsPen, Is.True);
            Assert.That(Moneda.PEN.EsUsd, Is.False);

            Assert.That(Moneda.USD.Codigo, Is.EqualTo("USD"));
            Assert.That(Moneda.USD.Simbolo, Is.EqualTo("US$"));
            Assert.That(Moneda.USD.Decimales, Is.EqualTo(2));
            Assert.That(Moneda.USD.EsMonedaNacional, Is.False);
            Assert.That(Moneda.USD.EsUsd, Is.True);
            Assert.That(Moneda.USD.EsPen, Is.False);

            var set = new HashSet<Moneda>(Moneda.All);
            Assert.That(set, Has.Member(Moneda.PEN));
            Assert.That(set, Has.Member(Moneda.USD));
            Assert.That(set.Count, Is.EqualTo(2));
        }

        [Test]
        public void FromCode_Valido_RetornaInstanciaCanonica()
        {
            var m1 = Moneda.FromCode("PEN");
            var m2 = Moneda.FromCode("USD");
            Assert.That(m1, Is.SameAs(Moneda.PEN));
            Assert.That(m2, Is.SameAs(Moneda.USD));
        }

        [Test]
        public void FromCode_Invalido_Lanza()
        {
            Assert.That(() => Moneda.FromCode("EUR"), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Moneda.FromCode(""), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => Moneda.FromCode(null!), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void From_AceptaAliasesHumanos()
        {
            Assert.That(Moneda.From("S/."), Is.EqualTo(Moneda.PEN));
            Assert.That(Moneda.From("soles"), Is.EqualTo(Moneda.PEN));
            Assert.That(Moneda.From("DÓLAR"), Is.EqualTo(Moneda.USD));
            Assert.That(Moneda.From("us$"), Is.EqualTo(Moneda.USD));
        }

        [Test]
        public void From_Desconocido_Lanza()
        {
            Assert.That(() => Moneda.From("yen"), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Moneda.From(null!), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TryParse_ComportamientoEsperado()
        {
            Assert.That(Moneda.TryParse("PEN", out var a), Is.True);
            Assert.That(a, Is.EqualTo(Moneda.PEN));

            Assert.That(Moneda.TryParse("dólares", out var b), Is.True);
            Assert.That(b, Is.EqualTo(Moneda.USD));

            Assert.That(Moneda.TryParse("eur", out _), Is.False);
            Assert.That(Moneda.TryParse("", out _), Is.False);
            Assert.That(Moneda.TryParse(null, out _), Is.False);
        }

        [Test]
        public void TienePrecisionValida_RespetaDecimales()
        {
            // PEN/USD => 2 decimales
            Assert.That(Moneda.PEN.TienePrecisionValida(10m), Is.True);
            Assert.That(Moneda.PEN.TienePrecisionValida(10.1m), Is.True);
            Assert.That(Moneda.PEN.TienePrecisionValida(10.12m), Is.True);
            Assert.That(Moneda.PEN.TienePrecisionValida(10.123m), Is.False);

            // Negativos y cero
            Assert.That(Moneda.USD.TienePrecisionValida(-0.99m), Is.True);
            Assert.That(Moneda.USD.TienePrecisionValida(-0.999m), Is.False);
            Assert.That(Moneda.USD.TienePrecisionValida(0m), Is.True);
        }

        [Test]
        public void IgualdadPorValor_OperadoresYHashCode()
        {
            var x = Moneda.FromCode("PEN");
            var y = Moneda.From("sol");
            var z = Moneda.FromCode("USD");

            Assert.That(x.Equals(y), Is.True);
            Assert.That(x == y, Is.True);
            Assert.That(x != y, Is.False);
            Assert.That(x.Equals(z), Is.False);
            Assert.That(x.GetHashCode(), Is.EqualTo(y.GetHashCode()));
        }

        [Test]
        public void ToString_Y_Conversiones()
        {
            var m = Moneda.PEN;
            Assert.That(m.ToString(), Is.EqualTo("PEN"));

            string code = m; // implícito
            Assert.That(code, Is.EqualTo("PEN"));

            var fromAlias = (Moneda)"us$"; // explícito
            Assert.That(fromAlias, Is.EqualTo(Moneda.USD));
        }
    }
}
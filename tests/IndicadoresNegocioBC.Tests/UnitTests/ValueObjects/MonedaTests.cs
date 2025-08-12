using System;
using IndicadoresNegocioBC.Domain.ValueObjects;
using NUnit.Framework;

namespace IndicadoresNegocioBC.Tests.UnitTests.ValueObjects
{
    public class MonedaTests
    {
        [Test]
        public void Crear_CodigoUppercaseValido_Aceptado()
        {
            var m = new Moneda("PEN");
            Assert.That(m.Codigo, Is.EqualTo("PEN"));
        }

        [Test]
        public void Crear_CodigoNulo_LanzaArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Moneda(null!));
        }

        [Test]
        public void Crear_CodigoVacio_LanzaArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new Moneda(string.Empty));
        }

        [Test]
        public void Crear_CodigoSoloEspacios_LanzaArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new Moneda("   "));
        }

        [Test]
        public void Crear_LongitudDistintaDeTres_LanzaArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new Moneda("PE"));   // < 3
            Assert.Throws<ArgumentException>(() => new Moneda("PENN")); // > 3
        }

        [Test]
        public void Crear_CodigoMinusculas_NoNormaliza_LanzaArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new Moneda("pen"));
        }

        [Test]
        public void Crear_CodigoConEspacios_NoNormaliza_LanzaArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new Moneda(" PEN "));
        }

        [Test]
        public void Crear_CaracteresInvalidos_LanzaArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new Moneda("P3N")); // dígito
            Assert.Throws<ArgumentException>(() => new Moneda("P*N")); // símbolo
            Assert.Throws<ArgumentException>(() => new Moneda("ÑEN")); // fuera de A-Z ASCII
        }

        [Test]
        public void Igualdad_PorValor_MismoCodigo()
        {
            var a = new Moneda("USD");
            var b = new Moneda("USD");
            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void Desigualdad_CodigosDistintos()
        {
            var a = new Moneda("PEN");
            var b = new Moneda("USD");
            Assert.That(a, Is.Not.EqualTo(b));
        }

        [Test]
        public void ToString_DevuelveCodigo()
        {
            var m = new Moneda("PEN");
            Assert.That(m.ToString(), Is.EqualTo("PEN"));
        }

        [Test]
        public void Fabrica_Crear_EquivalenteAlConstructor()
        {
            var a = new Moneda("EUR");
            var b = Moneda.Crear("EUR");
            Assert.That(a, Is.EqualTo(b));
            Assert.That(b.Codigo, Is.EqualTo("EUR"));
        }
    }
}
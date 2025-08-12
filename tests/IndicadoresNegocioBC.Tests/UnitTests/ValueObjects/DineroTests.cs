using System;
using IndicadoresNegocioBC.Domain.ValueObjects;
using NUnit.Framework;

namespace IndicadoresNegocioBC.Tests.UnitTests.ValueObjects
{
    public class MonedaTests
    {
        [Test]
        public void Crear_CodigoValido_NormalizaAMayusculas()
        {
            var m = new Moneda("pen");
            Assert.That(m.Codigo, Is.EqualTo("PEN"));
        }

        [Test]
        public void Crear_CodigoConEspacios_TrimYMayusculas()
        {
            var m = new Moneda("  usd  ");
            Assert.That(m.Codigo, Is.EqualTo("USD"));
        }

        [Test]
        public void Crear_CodigoNuloOVacio_LanzaArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new Moneda(null!));
            Assert.Throws<ArgumentException>(() => new Moneda(string.Empty));
            Assert.Throws<ArgumentException>(() => new Moneda("   "));
        }

        [Test]
        public void Crear_CodigoLongitudDistintaDeTres_LanzaArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new Moneda("PE"));
            Assert.Throws<ArgumentException>(() => new Moneda("PENN"));
        }

        [Test]
        public void Crear_CodigoConCaracteresInvalidos_LanzaArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new Moneda("P3N"));  // dígito
            Assert.Throws<ArgumentException>(() => new Moneda("P*N"));  // símbolo
            Assert.Throws<ArgumentException>(() => new Moneda("ÑEN"));  // fuera de A-Z ASCII
        }

        [Test]
        public void Igualdad_PorValor_MismoCodigo()
        {
            var a = new Moneda("PEN");
            var b = new Moneda("pen");
            Assert.That(a, Is.EqualTo(b)); // record: igualdad por valor
            Assert.That(a.Codigo, Is.EqualTo("PEN"));
            Assert.That(b.Codigo, Is.EqualTo("PEN"));
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
            var m = new Moneda("eur");
            Assert.That(m.ToString(), Is.EqualTo("EUR"));
        }

        [Test]
        public void Fabrica_Crear_FuncionaIgualQueConstructor()
        {
            var a = new Moneda("gbp");
            var b = Moneda.Crear("gbp");
            Assert.That(a, Is.EqualTo(b));
            Assert.That(b.Codigo, Is.EqualTo("GBP"));
        }
    }
}
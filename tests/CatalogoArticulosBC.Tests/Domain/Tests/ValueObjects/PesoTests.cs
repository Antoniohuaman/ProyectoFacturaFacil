using System;
using NUnit.Framework;
using CatalogoArticulosBC.Domain.ValueObjects;

namespace CatalogoArticulosBC.Domain.Tests.ValueObjects
{
    [TestFixture]
    public class PesoTests
    {
        // -------------------- Construcción --------------------

        [Test]
        public void Ctor_ValorCero_Acepta()
        {
            var p = new Peso(0m);

            Assert.That(p.Valor, Is.EqualTo(0m));
        }

        [TestCase(0.001)]
        [TestCase(1)]
        [TestCase(12.345)]
        public void Ctor_ValorPositivo_Acepta(decimal valor)
        {
            var p = new Peso(valor);

            Assert.That(p.Valor, Is.EqualTo(valor));
        }

        [TestCase(-0.0001)]
        [TestCase(-1)]
        public void Ctor_Negativo_LanzaArgumentOutOfRange(decimal valor)
        {
            TestDelegate act = () => _ = new Peso(valor);

            Assert.That(act, Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("valor")
                .And.Message.Contains("negativo"));
        }

        // -------------------- Igualdad & Hash --------------------

        [Test]
        public void Igualdad_MismoValor_True_YHashIgual()
        {
            var a = new Peso(2.50m);
            var b = new Peso(2.500m); // mismo valor numérico

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void Desigualdad_ValoresDiferentes()
        {
            var a = new Peso(2.50m);
            var b = new Peso(2.49m);

            Assert.That(a, Is.Not.EqualTo(b));
            Assert.That(a.Equals(b), Is.False);
        }

        [Test]
        public void Equals_ContraNull_EsFalso()
        {
            var a = new Peso(1m);

            Assert.That(a.Equals(null), Is.False);
        }

        // -------------------- ToString (formato) --------------------

        [Test]
        public void ToString_FormateaConDosDecimalesYUnidad()
        {
            var p = new Peso(1m);
            Assert.That(p.ToString(), Is.EqualTo("1.00 kg"));
        }

        [Test]
        public void ToString_RellenaCerosDecimales()
        {
            var p = new Peso(1.2m);
            Assert.That(p.ToString(), Is.EqualTo("1.20 kg"));
        }

        [Test]
        public void ToString_RedondeaHaciaFormatoF2()
        {
            var p1 = new Peso(1.236m);   // -> 1.24
            var p2 = new Peso(0.004m);   // -> 0.00

            Assert.That(p1.ToString(), Is.EqualTo("1.24 kg"));
            Assert.That(p2.ToString(), Is.EqualTo("0.00 kg"));
        }
    }
}

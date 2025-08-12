using System;
using IndicadoresNegocioBC.Domain.ValueObjects;
using NUnit.Framework;

namespace IndicadoresNegocioBC.Tests.UnitTests.ValueObjects
{
    public class LimiteTopTests
    {
        [TestCase(1)]
        [TestCase(10)]
        [TestCase(100)]
        public void Crear_ValorValido_RetornaInstanciaConValor(int valor)
        {
            var lt = LimiteTop.Crear(valor);

            Assert.That(lt.Valor, Is.EqualTo(valor));
        }

        [Test]
        public void Crear_ValorMenorQueUno_LanzaArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => LimiteTop.Crear(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => LimiteTop.Crear(-5));
        }

        [Test]
        public void Crear_ValorMayorQueMaximoPermitido_LanzaArgumentOutOfRangeException()
        {
            var mayor = LimiteTop.MaximoPermitido + 1;
            Assert.Throws<ArgumentOutOfRangeException>(() => LimiteTop.Crear(mayor));
        }

        [Test]
        public void Singletons_TienenValoresEsperados()
        {
            Assert.Multiple(() =>
            {
                Assert.That(LimiteTop.Top5.Valor, Is.EqualTo(5));
                Assert.That(LimiteTop.Top10.Valor, Is.EqualTo(10));
                Assert.That(LimiteTop.Top20.Valor, Is.EqualTo(20));
                Assert.That(LimiteTop.Top50.Valor, Is.EqualTo(50));
            });
        }

        [Test]
        public void Igualdad_PorValor_Record_ComparaCorrectamente()
        {
            var a = LimiteTop.Crear(10);
            var b = LimiteTop.Top10; // mismo valor

            Assert.That(a, Is.EqualTo(b)); // igualdad por valor (record)
        }

        [Test]
        public void DesdeConTope_SinMaximoYExcede_DefaultCapAlMaximoPermitido()
        {
            var lt = LimiteTop.DesdeConTope(LimiteTop.MaximoPermitido + 500, null);

            Assert.That(lt.Valor, Is.EqualTo(LimiteTop.MaximoPermitido));
        }

        [Test]
        public void DesdeConTope_ConMaximoPersonalizado_AplicaTope()
        {
            var lt = LimiteTop.DesdeConTope(30, maximo: 25);

            Assert.That(lt.Valor, Is.EqualTo(25));
        }

        [Test]
        public void DesdeConTope_ConMaximoPersonalizado_ValorDentroDelTope_RespetaValor()
        {
            var lt = LimiteTop.DesdeConTope(15, maximo: 25);

            Assert.That(lt.Valor, Is.EqualTo(15));
        }

        [Test]
        public void DesdeConTope_MaximoInvalido_UsaMaximoPermitido()
        {
            // maximo <= 0 se considera inválido y cae al MaximoPermitido
            var lt = LimiteTop.DesdeConTope(500, maximo: 0);

            Assert.That(lt.Valor, Is.EqualTo(LimiteTop.MaximoPermitido));
        }

        [Test]
        public void DesdeConTope_ValorMenorQueUno_LanzaArgumentOutOfRangeException()
        {
            // No hay "autocorrección" para valores < 1; debe lanzar
            Assert.Throws<ArgumentOutOfRangeException>(() => LimiteTop.DesdeConTope(0, maximo: 10));
        }

        [Test]
        public void ToString_FormatoCorrecto()
        {
            Assert.That(LimiteTop.Top10.ToString(), Is.EqualTo("Top 10"));
            Assert.That(LimiteTop.Crear(7).ToString(), Is.EqualTo("Top 7"));
        }
    }
}
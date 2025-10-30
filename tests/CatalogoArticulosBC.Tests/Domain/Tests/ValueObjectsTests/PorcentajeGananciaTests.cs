#nullable enable
using System;
using NUnit.Framework;
using CatalogoArticulosBC.Domain.ValueObjects;

namespace CatalogoArticulosBC.Tests.Domain.Tests.ValueObjectsTests
{
    [TestFixture]
    public class PorcentajeGananciaTests
    {
        [Test]
        public void Desde_Acepta_0_y_100_Inclusive()
        {
            Assert.That(PorcentajeGanancia.Desde(0m).Valor, Is.EqualTo(0m));
            Assert.That(PorcentajeGanancia.Desde(100m).Valor, Is.EqualTo(100m));
        }

        [Test]
        public void Rechaza_fuera_de_rango()
        {
            Assert.That(() => PorcentajeGanancia.Desde(-0.01m), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => PorcentajeGanancia.Desde(100.01m), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Redondea_A_2_Decimales_AwayFromZero()
        {
            var p = PorcentajeGanancia.Desde(12.345m);
            Assert.That(p.Valor, Is.EqualTo(12.35m));
        }

        [Test]
        public void DesdeFraccion_Convierte_Correctamente()
        {
            var p = PorcentajeGanancia.DesdeFraccion(0.2m);
            Assert.That(p.Valor, Is.EqualTo(20m));
        }

        [Test]
        public void ComoFraccion_Devuelve_Valor_Entre_0_y_1()
        {
            var p = PorcentajeGanancia.Desde(7.5m);
            Assert.That(p.ComoFraccion(), Is.EqualTo(0.075m));
        }

        [Test]
        public void Igualdad_Por_Valor()
        {
            var a = PorcentajeGanancia.Desde(10m);
            var b = PorcentajeGanancia.Desde(10.00m);
            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void ToString_Formatea_Con_Signo_Porcentaje()
        {
            var p = PorcentajeGanancia.Desde(7.5m);
            Assert.That(p.ToString(), Is.EqualTo("7.5%"));
        }
    }
}

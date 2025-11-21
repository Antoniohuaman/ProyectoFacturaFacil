using System;
using ListaPreciosBC.Domain.ValueObjects;
using NUnit.Framework;

namespace ListaPreciosBC.Tests.Domain.Tests.ValueObjectsTests
{
    [TestFixture]
    public class PorcentajeDescuentoPaqueteTests
    {
        [Test]
        public void Crear_ValorDentroDeRango_Ok()
        {
            var descuento = PorcentajeDescuentoPaquete.Crear(20m);

            Assert.That(descuento.Valor, Is.EqualTo(20m));
        }

        [Test]
        public void Crear_ValorMenorQueCero_LanzaArgumentOutOfRangeException()
        {
            Assert.That(
                () => PorcentajeDescuentoPaquete.Crear(-1m),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Crear_ValorMayorQueCien_LanzaArgumentOutOfRangeException()
        {
            Assert.That(
                () => PorcentajeDescuentoPaquete.Crear(101m),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void CalcularDescuento_SobreMontoBase_DevuelveMontoCorrecto()
        {
            var descuento = PorcentajeDescuentoPaquete.Crear(20m);

            var monto = descuento.CalcularDescuento(150m);

            Assert.That(monto, Is.EqualTo(30m));
        }

        [Test]
        public void CalcularDescuento_MontoBaseNegativo_LanzaArgumentOutOfRangeException()
        {
            var descuento = PorcentajeDescuentoPaquete.Crear(10m);

            Assert.That(
                () => descuento.CalcularDescuento(-1m),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}

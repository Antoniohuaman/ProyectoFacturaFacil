using System;
using ListaPreciosBC.Domain.ValueObjects;
using NUnit.Framework;

namespace ListaPreciosBC.Tests.Domain.Tests.ValueObjectsTests
{
    [TestFixture]
    public class CantidadProductoPaqueteTests
    {
        [Test]
        public void Crear_CantidadValida_DevuelveInstancia()
        {
            var cantidad = CantidadProductoPaquete.Crear(3);

            Assert.That(cantidad.Valor, Is.EqualTo(3));
        }

        [Test]
        public void Crear_CantidadCero_LanzaArgumentOutOfRangeException()
        {
            Assert.That(
                () => CantidadProductoPaquete.Crear(0),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Crear_CantidadNegativa_LanzaArgumentOutOfRangeException()
        {
            Assert.That(
                () => CantidadProductoPaquete.Crear(-1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Incrementar_Y_Decrementar_MantienenInvariantes()
        {
            var cantidad = CantidadProductoPaquete.Crear(5);

            var incrementada = cantidad.Incrementar(2);
            Assert.That(incrementada.Valor, Is.EqualTo(7));

            var decrementada = incrementada.Decrementar(2);
            Assert.That(decrementada.Valor, Is.EqualTo(5));
        }

        [Test]
        public void Decrementar_DejaCantidadMenorOIgualCero_LanzaInvalidOperationException()
        {
            var cantidad = CantidadProductoPaquete.Crear(2);

            Assert.That(
                () => cantidad.Decrementar(2),
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Equals_MismasCantidades_True()
        {
            var a = CantidadProductoPaquete.Crear(2);
            var b = CantidadProductoPaquete.Crear(2);

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a == b, Is.True);
        }
    }
}

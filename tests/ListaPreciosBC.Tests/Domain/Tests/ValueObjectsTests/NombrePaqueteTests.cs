using System;
using ListaPreciosBC.Domain.ValueObjects;
using NUnit.Framework;

namespace ListaPreciosBC.Tests.Domain.Tests.ValueObjectsTests
{
    [TestFixture]
    public class NombrePaqueteTests
    {
        [Test]
        public void Crear_NombreValido_NormalizaYConservaValor()
        {
            var nombre = NombrePaquete.Crear("  Canasta Navideña  ");

            Assert.That(nombre.Valor, Is.EqualTo("Canasta Navideña"));
        }

        [Test]
        public void Crear_NombreNulo_LanzaArgumentNullException()
        {
            Assert.That(
                () => NombrePaquete.Crear(null!),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Crear_NombreVacio_LanzaArgumentException()
        {
            Assert.That(
                () => NombrePaquete.Crear("   "),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Crear_NombreMuyLargo_LanzaArgumentException()
        {
            var textoLargo = new string('x', 101);

            Assert.That(
                () => NombrePaquete.Crear(textoLargo),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Equals_MismosValores_True()
        {
            var a = NombrePaquete.Crear("Canasta");
            var b = NombrePaquete.Crear("Canasta");

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a == b, Is.True);
        }
    }
}

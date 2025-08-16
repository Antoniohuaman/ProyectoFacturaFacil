using System;
using NUnit.Framework;
using SharedKernel.ValueObjects;

namespace SharedKernel.Tests.UnitTests.ValueObjects
{
    [TestFixture]
    public class CentroDeCostoTests
    {
        [Test]
        public void CrearCentroDeCosto_Valido_NoLanzaExcepcion()
        {
            var centro = new CentroDeCosto("CC001", "Centro de Producción");
            Assert.That(centro.Code, Is.EqualTo("CC001"));
            Assert.That(centro.Name, Is.EqualTo("Centro de Producción"));
        }

        [Test]
        public void CrearCentroDeCosto_CodigoVacio_LanzaExcepcion()
        {
            Assert.Throws<ArgumentException>(() => new CentroDeCosto("", "Nombre"));
        }

        [Test]
        public void CrearCentroDeCosto_NombreVacio_LanzaExcepcion()
        {
            Assert.Throws<ArgumentException>(() => new CentroDeCosto("CC001", ""));
        }

        [Test]
        public void CrearCentroDeCosto_CodigoConCaracteresInvalidos_LanzaExcepcion()
        {
            Assert.Throws<ArgumentException>(() => new CentroDeCosto("CC@01", "Nombre"));
        }

        [Test]
        public void CrearCentroDeCosto_CodigoMuyLargo_LanzaExcepcion()
        {
            var largo = new string('A', CentroDeCosto.MaxCodeLength + 1);
            Assert.Throws<ArgumentException>(() => new CentroDeCosto(largo, "Nombre"));
        }

        [Test]
        public void CrearCentroDeCosto_NombreMuyLargo_LanzaExcepcion()
        {
            var largo = new string('A', CentroDeCosto.MaxNameLength + 1);
            Assert.Throws<ArgumentException>(() => new CentroDeCosto("CC001", largo));
        }
    }
}

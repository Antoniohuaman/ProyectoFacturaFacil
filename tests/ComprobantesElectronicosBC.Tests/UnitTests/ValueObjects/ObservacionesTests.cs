using System;
using NUnit.Framework;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.UnitTests.ValueObjects
{
    public class ObservacionesTests
    {
        [Test]
        public void Create_Recorta_NormalizaSaltosYFiltraControles()
        {
            // Incluye CRLF, tab y un control no imprimible (\u0001) que debe eliminarse
            var raw = "  Hola\r\nMundo\u0001\t  ";

            var obs = Observaciones.Create(raw);

            // CRLF -> LF, se elimina \u0001, se hace Trim (tabulador final eliminado)
            Assert.That(obs.Texto, Is.EqualTo("Hola\nMundo"));
        }

        [Test]
        public void Create_Vacio_LanzaArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() => Observaciones.Create("   "));
            Assert.That(ex!.Message, Does.Contain("Observaciones").IgnoreCase);
        }

        [Test]
        public void Create_ExcedeMaxLength_LanzaArgumentException()
        {
            var largo = new string('x', Observaciones.MaxLength + 1);
            var ex = Assert.Throws<ArgumentException>(() => Observaciones.Create(largo));
            Assert.That(ex!.Message, Does.Contain("exced").IgnoreCase); // exceder longitud
        }

        [Test]
        public void FromOptional_NullOWhitespace_DevuelveNull()
        {
            Assert.That(Observaciones.FromOptional(null), Is.Null);
            Assert.That(Observaciones.FromOptional("   "), Is.Null);
        }

        [Test]
        public void FromOptional_ConTextoValido_DevuelveInstanciaNormalizada()
        {
            var obs = Observaciones.FromOptional("  Gracias\rpor su compra  ");
            Assert.That(obs, Is.Not.Null);
            Assert.That(obs!.Texto, Is.EqualTo("Gracias\npor su compra"));
        }

        [Test]
        public void ToString_DevuelveTexto()
        {
            var obs = Observaciones.Create("Entrega coordinada 3pm");
            Assert.That(obs.ToString(), Is.EqualTo("Entrega coordinada 3pm"));
        }
    }
}

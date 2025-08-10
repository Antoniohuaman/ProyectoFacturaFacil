using System;
using NUnit.Framework;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.UnitTests.ValueObjects
{
    public class NotaInternaTests
    {
        [Test]
        public void Create_Ok_TrimsCampos_YUsaFechaInyectada()
        {
            var ts = new DateTimeOffset(2025, 08, 01, 12, 30, 00, TimeSpan.Zero);

            var nota = NotaInterna.Create("  Cliente feliz  ", "  Mabel  ", ts);

            Assert.Multiple(() =>
            {
                Assert.That(nota.Texto, Is.EqualTo("Cliente feliz"));
                Assert.That(nota.Autor, Is.EqualTo("Mabel"));
                Assert.That(nota.CreadaEnUtc, Is.EqualTo(ts));
            });
        }

        [Test]
        public void Create_AutorEnBlanco_QuedaNull()
        {
            var nota = NotaInterna.Create("Algo de texto", "   ");
            Assert.That(nota.Autor, Is.Null);
        }

        [Test]
        public void Create_Lanza_SiTextoNullOVacio()
        {
            Assert.Throws<ArgumentNullException>(() => NotaInterna.Create(null!));
            var ex = Assert.Throws<ArgumentException>(() => NotaInterna.Create("   "));
            Assert.That(ex!.ParamName, Is.EqualTo("texto"));
        }

        [Test]
        public void Create_Lanza_SiTextoMuyLargo()
        {
            var t = new string('A', 1001); // límite en tu VO: 1000
            var ex = Assert.Throws<ArgumentException>(() => NotaInterna.Create(t));
            Assert.That(ex!.ParamName, Is.EqualTo("texto"));
        }

        [Test]
        public void Create_Lanza_SiAutorMuyLargo()
        {
            var a = new string('Z', 101); // límite en tu VO: 100
            var ex = Assert.Throws<ArgumentException>(() => NotaInterna.Create("ok", a));
            Assert.That(ex!.ParamName, Is.EqualTo("autor"));
        }

        [Test]
        public void ToString_IncluyeAutor_SiExiste()
        {
            var conAutor = NotaInterna.Create("Observación", "Ana");
            Assert.That(conAutor.ToString(), Does.Contain("Observación"));
            Assert.That(conAutor.ToString(), Does.Contain("Ana"));

            var sinAutor = NotaInterna.Create("Solo texto");
            Assert.That(sinAutor.ToString(), Is.EqualTo("Solo texto"));
        }
    }
}

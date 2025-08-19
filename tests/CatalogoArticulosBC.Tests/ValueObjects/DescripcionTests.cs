using NUnit.Framework;
using CatalogoArticulosBC.Domain.ValueObjects;
using System;

namespace CatalogoArticulosBC.Tests.ValueObjects
{
    [TestFixture]
    public class DescripcionTests
    {
        [Test]
        public void CrearDescripcion_Valida_NoLanzaExcepcion()
        {
            var texto = "Producto de alta calidad #123!";
            var descripcion = Descripcion.From(texto);
            Assert.That(descripcion.Texto, Is.EqualTo(texto.Trim()));
        }

        [Test]
        public void CrearDescripcion_Nula_LanzaExcepcion()
        {
            var descripcion = Descripcion.From(null);
            Assert.That(descripcion.Texto, Is.EqualTo(string.Empty));
        }

        [Test]
        public void CrearDescripcion_Vacia_LanzaExcepcion()
        {
            var descripcion = Descripcion.From("   ");
            Assert.That(descripcion.Texto, Is.EqualTo(string.Empty));
        }

        [Test]
        public void CrearDescripcion_ExcedeLongitud_LanzaExcepcion()
        {
            var texto = new string('a', 501);
            Assert.Throws<ArgumentException>(() => Descripcion.From(texto));
        }

        [Test]
        public void CrearDescripcion_ConEspacios_SeNormaliza()
        {
            var texto = "   Descripción con espacios   ";
            var descripcion = Descripcion.From(texto);
            Assert.That(descripcion.Texto, Is.EqualTo("Descripción con espacios"));
        }

        [Test]
        public void Descripcion_IgualdadFunciona()
        {
            var d1 = Descripcion.From("Producto X");
            var d2 = Descripcion.From("Producto X");
            Assert.That(d1, Is.EqualTo(d2));
            Assert.That(d1.Equals(d2), Is.True);
        }

        [Test]
        public void Descripcion_DiferenteFunciona()
        {
            var d1 = Descripcion.From("Producto X");
            var d2 = Descripcion.From("Producto Y");
            Assert.That(d1, Is.Not.EqualTo(d2));
            Assert.That(d1.Equals(d2), Is.False);
        }
    }
}

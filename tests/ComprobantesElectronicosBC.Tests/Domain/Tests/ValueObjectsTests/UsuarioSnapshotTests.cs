using System;
using NUnit.Framework;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.ValueObjects
{
    [TestFixture]
    public class UsuarioSnapshotTests
    {
        [Test]
        public void Ctor_ValoresValidos_CreaInstancia_Y_TrimmeaCampos()
        {
            // Act
            var usuario = new UsuarioSnapshot("  u1  ", "  Ana Pérez  ", "  Cajero  ");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(usuario.Codigo, Is.EqualTo("u1"));
                Assert.That(usuario.NombreCompleto, Is.EqualTo("Ana Pérez"));
                Assert.That(usuario.Rol, Is.EqualTo("Cajero"));
                Assert.That(usuario.ToString(), Is.EqualTo("Ana Pérez (Cajero)"));
            });
        }

        [TestCase(null, "Ana", "Rol", "codigo")]
        [TestCase("", "Ana", "Rol", "codigo")]
        [TestCase("   ", "Ana", "Rol", "codigo")]
        [TestCase("u1", null, "Rol", "nombreCompleto")]
        [TestCase("u1", "", "Rol", "nombreCompleto")]
        [TestCase("u1", "   ", "Rol", "nombreCompleto")]
        [TestCase("u1", "Ana", null, "rol")]
        [TestCase("u1", "Ana", "", "rol")]
        [TestCase("u1", "Ana", "   ", "rol")]
        public void Ctor_NullOVacio_LanzaArgumentExceptionConParamName(
            string codigo, string nombreCompleto, string rol, string esperadoParamName)
        {
            // Act
            var ex = Assert.Throws<ArgumentException>(() =>
                new UsuarioSnapshot(codigo, nombreCompleto, rol));

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(ex!.ParamName, Is.EqualTo(esperadoParamName));
                Assert.That(ex.Message, Does.Contain("obligatorio"));
            });
        }

        [Test]
        public void Record_IgualdadPorValor_Funciona()
        {
            var a = new UsuarioSnapshot("u1", "Ana", "Vendedor");
            var b = new UsuarioSnapshot("u1", "Ana", "Vendedor");
            var c = new UsuarioSnapshot("u2", "Ana", "Vendedor");

            Assert.Multiple(() =>
            {
                Assert.That(a, Is.EqualTo(b));
                Assert.That(a == b, Is.True);
                Assert.That(a, Is.Not.EqualTo(c));
                Assert.That(a != c, Is.True);
            });
        }

        [Test]
        public void WithExpression_GeneraCopiaIndependiente()
        {
            var original = new UsuarioSnapshot("u1", "Ana", "Vendedor");

            // Con records, el 'with' permite crear una copia modificando propiedades init en la copia
            var copia = original with { Rol = "Cajero" };

            Assert.Multiple(() =>
            {
                Assert.That(copia.Rol, Is.EqualTo("Cajero"));
                Assert.That(original.Rol, Is.EqualTo("Vendedor")); // se mantiene inmutable
                Assert.That(copia, Is.Not.EqualTo(original));
            });
        }
    }
}

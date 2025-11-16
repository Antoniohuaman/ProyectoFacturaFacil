// tests/GestionClientesBC.Tests/ValueObjects/FotoPerfilClienteTests.cs
using GestionClientesBC.Domain.ValueObjects;
using NUnit.Framework;

namespace GestionClientesBC.Tests.ValueObjects
{
    public class FotoPerfilClienteTests
    {
        [Test]
        public void Create_WhenBothNull_ReturnsVacio()
        {
            var foto = FotoPerfilCliente.Create(null, null);

            Assert.That(foto, Is.SameAs(FotoPerfilCliente.Vacio));
            Assert.That(foto.TieneFoto, Is.False);
        }

        [Test]
        public void Create_TrimAndNormalize()
        {
            var foto = FotoPerfilCliente.Create("  avatar.png  ", "  https://ejemplo.com/img  ");

            Assert.That(foto.NombreArchivo, Is.EqualTo("avatar.png"));
            Assert.That(foto.UrlPublica, Is.EqualTo("https://ejemplo.com/img"));
        }

        [Test]
        public void Equality_IsValueBased()
        {
            var a = FotoPerfilCliente.Create("foto.png", "https://example.com/foto.png");
            var b = FotoPerfilCliente.Create("foto.png", "https://example.com/foto.png");

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }
    }
}

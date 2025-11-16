// tests/GestionClientesBC.Tests/ValueObjects/PaginaWebClienteTests.cs
using GestionClientesBC.Domain.ValueObjects;
using NUnit.Framework;

namespace GestionClientesBC.Tests.ValueObjects
{
    public class PaginaWebClienteTests
    {
        [Test]
        public void Create_NullOrWhitespace_ReturnsNull()
        {
            Assert.That(PaginaWebCliente.Create(null), Is.Null);
            Assert.That(PaginaWebCliente.Create("   "), Is.Null);
        }

        [Test]
        public void Create_TrimsAndLimitsLength()
        {
            var textoLargo = new string('w', 300) + ".com";

            var web = PaginaWebCliente.Create("  " + textoLargo + "  ")!;

            Assert.That(web.Valor.Length, Is.EqualTo(200));
        }

        [Test]
        public void Equality_IgnoresCase()
        {
            var a = PaginaWebCliente.Create("https://miempresa.com")!;
            var b = PaginaWebCliente.Create("HTTPS://MIEMPRESA.COM")!;

            Assert.That(a, Is.EqualTo(b));
        }
    }
}

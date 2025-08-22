using ListaPreciosBC.Domain.Policies;
using ListaPreciosBC.Domain.ValueObjects;
using NUnit.Framework;

namespace ListaPreciosBC.Tests.UnitTests.Policies
{
    [TestFixture]
    public class PuedeCambiarModoColumnaPolicyTests
    {
        [Test]
        public void Validar_ModoValido_RetornaTrue()
        {
            Assert.That(PuedeCambiarModoColumnaPolicy.Validar(ModoValorizacionColumna.Fijo.ToString()), Is.True);
            Assert.That(PuedeCambiarModoColumnaPolicy.Validar("PorVolumen"), Is.True);
        }

        [Test]
        public void Validar_ModoInvalido_RetornaFalse()
        {
            Assert.That(PuedeCambiarModoColumnaPolicy.Validar("Invalido"), Is.False);
            Assert.That(PuedeCambiarModoColumnaPolicy.Validar(string.Empty), Is.False);
        }
    }
}

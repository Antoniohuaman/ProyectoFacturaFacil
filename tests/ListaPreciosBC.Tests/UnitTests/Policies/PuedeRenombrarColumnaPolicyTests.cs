using ListaPreciosBC.Domain.Policies;
using NUnit.Framework;

namespace ListaPreciosBC.Tests.UnitTests.Policies
{
    [TestFixture]
    public class PuedeRenombrarColumnaPolicyTests
    {
        [Test]
        public void Validar_NombreValido_RetornaTrue()
        {
            Assert.That(PuedeRenombrarColumnaPolicy.Validar("Columna1"), Is.True);
        }

        [Test]
        public void Validar_NombreInvalido_RetornaFalse()
        {
            Assert.That(PuedeRenombrarColumnaPolicy.Validar(""), Is.False);
        }
    }
}

using NUnit.Framework;
using ListaPreciosBC.Domain.Policies;

namespace ListaPreciosBC.Tests.Domain.Tests.BusinessRulesTests.PoliciesTests
{
    [TestFixture]
    public class PuedeCambiarModoColumnaPolicyTests
    {
        [Test]
        public void Validar_DeberiaRetornarTrue_CuandoModoEsFijo()
        {
            var resultado = PuedeCambiarModoColumnaPolicy.Validar("Fijo");
            Assert.That(resultado, Is.True);
        }

        [Test]
        public void Validar_DeberiaRetornarTrue_CuandoModoEsPorVolumen()
        {
            var resultado = PuedeCambiarModoColumnaPolicy.Validar("PorVolumen");
            Assert.That(resultado, Is.True);
        }

        [Test]
        public void Validar_DeberiaRetornarTrue_CuandoModoEsCodigoF()
        {
            var resultado = PuedeCambiarModoColumnaPolicy.Validar("F");
            Assert.That(resultado, Is.True);
        }

        [Test]
        public void Validar_DeberiaRetornarTrue_CuandoModoEsCodigoV()
        {
            var resultado = PuedeCambiarModoColumnaPolicy.Validar("V");
            Assert.That(resultado, Is.True);
        }

        [Test]
        public void Validar_DeberiaRetornarFalse_CuandoModoEsInvalido()
        {
            var resultado = PuedeCambiarModoColumnaPolicy.Validar("Descuento");
            Assert.That(resultado, Is.False);
        }

        [Test]
        public void Validar_DeberiaRetornarFalse_CuandoModoEsVacio()
        {
            var resultado = PuedeCambiarModoColumnaPolicy.Validar("");
            Assert.That(resultado, Is.False);
        }
    }
}

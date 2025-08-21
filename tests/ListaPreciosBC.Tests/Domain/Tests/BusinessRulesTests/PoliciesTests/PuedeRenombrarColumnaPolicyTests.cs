using NUnit.Framework;
using ListaPreciosBC.Domain.Policies;

namespace ListaPreciosBC.Tests.Domain.Tests.BusinessRulesTests.PoliciesTests
{
    [TestFixture]
    public class PuedeRenombrarColumnaPolicyTests
    {
        [Test]
        public void Validar_DeberiaRetornarTrue_CuandoNombreEsValido()
        {
            var resultado = PuedeRenombrarColumnaPolicy.Validar("Precio Base");
            Assert.That(resultado, Is.True);
        }

        [Test]
        public void Validar_DeberiaRetornarFalse_CuandoNombreEsVacio()
        {
            var resultado = PuedeRenombrarColumnaPolicy.Validar("");
            Assert.That(resultado, Is.False);
        }

        [Test]
        public void Validar_DeberiaRetornarFalse_CuandoNombreEsMuyLargo()
        {
            var nombreLargo = new string('A', 101); // Asumiendo límite 100
            var resultado = PuedeRenombrarColumnaPolicy.Validar(nombreLargo);
            Assert.That(resultado, Is.False);
        }
        // El método Validar no acepta valores nulos por diseño de dominio. Si se pasa null, se genera advertencia de compilador.
        // Por eso, no se prueba el caso null aquí.
    }
}

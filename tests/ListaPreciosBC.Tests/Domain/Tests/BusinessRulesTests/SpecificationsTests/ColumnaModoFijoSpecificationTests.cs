using ListaPreciosBC.Domain.Specifications;
using ListaPreciosBC.Domain.ValueObjects;
using NUnit.Framework;

namespace ListaPreciosBC.Tests.Domain.Tests.BusinessRulesTests.SpecificationsTests
{
    [TestFixture]
    public class ColumnaModoFijoSpecificationTests
    {
        [Test]
        public void IsSatisfiedBy_ReturnsTrue_WhenModoIsPorVolumen()
        {
            var columna = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(3),
                NombreColumnaPrecio.Crear("Volumen"),
                ModoValorizacionColumna.PorVolumen,
                esBase: false,
                visible: true
            );
            var spec = new ColumnaModoFijoSpecification();
            Assert.That(spec.IsSatisfiedBy(columna), Is.True);
        }

        [Test]
        public void IsSatisfiedBy_ReturnsFalse_WhenModoIsFijo()
        {
            var columna = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(4),
                NombreColumnaPrecio.Crear("Fijo"),
                ModoValorizacionColumna.Fijo,
                esBase: false,
                visible: true
            );
            var spec = new ColumnaModoFijoSpecification();
            Assert.That(spec.IsSatisfiedBy(columna), Is.False);
        }
    }
}

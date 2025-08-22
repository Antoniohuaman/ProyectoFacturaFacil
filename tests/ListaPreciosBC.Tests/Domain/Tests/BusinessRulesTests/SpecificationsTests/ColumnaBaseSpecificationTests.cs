using ListaPreciosBC.Domain.Specifications;
using ListaPreciosBC.Domain.ValueObjects;
using NUnit.Framework;

namespace ListaPreciosBC.Tests.Domain.Tests.BusinessRulesTests.SpecificationsTests
{
    [TestFixture]
    public class ColumnaBaseSpecificationTests
    {
        [Test]
        public void IsSatisfiedBy_ReturnsTrue_WhenColumnaIsBase()
        {
            var columna = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(1),
                NombreColumnaPrecio.Crear("Base"),
                ModoValorizacionColumna.Fijo,
                esBase: true,
                visible: true
            );
            var spec = new ColumnaBaseSpecification();
            Assert.That(spec.IsSatisfiedBy(columna), Is.True);
        }

        [Test]
        public void IsSatisfiedBy_ReturnsFalse_WhenColumnaIsNotBase()
        {
            var columna = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(2),
                NombreColumnaPrecio.Crear("NoBase"),
                ModoValorizacionColumna.Fijo,
                esBase: false,
                visible: true
            );
            var spec = new ColumnaBaseSpecification();
            Assert.That(spec.IsSatisfiedBy(columna), Is.False);
        }
    }
}

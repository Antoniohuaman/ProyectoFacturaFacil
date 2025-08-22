using ListaPreciosBC.Domain.Specifications;
using ListaPreciosBC.Domain.ValueObjects;
using NUnit.Framework;

namespace ListaPreciosBC.Tests.Domain.Tests.BusinessRulesTests.SpecificationsTests
{
    [TestFixture]
    public class ColumnaPuedeSerRenombradaSpecificationTests
    {
        [Test]
        public void IsSatisfiedBy_ReturnsTrue_WhenColumnaIsVisibleAndNotBase()
        {
            var columna = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(1),
                NombreColumnaPrecio.Crear("Visible"),
                ModoValorizacionColumna.Fijo,
                esBase: false,
                visible: true
            );
            var spec = new ColumnaPuedeSerRenombradaSpecification();
            Assert.That(spec.IsSatisfiedBy(columna), Is.True);
        }

        [Test]
        public void IsSatisfiedBy_ReturnsFalse_WhenColumnaIsNotVisible()
        {
            var columna = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(2),
                NombreColumnaPrecio.Crear("Oculta"),
                ModoValorizacionColumna.Fijo,
                esBase: false,
                visible: false
            );
            var spec = new ColumnaPuedeSerRenombradaSpecification();
            Assert.That(spec.IsSatisfiedBy(columna), Is.False);
        }

        [Test]
        public void IsSatisfiedBy_ReturnsFalse_WhenColumnaIsBase()
        {
            var columna = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.DesdeNumero(3),
                NombreColumnaPrecio.Crear("Base"),
                ModoValorizacionColumna.Fijo,
                esBase: true,
                visible: true
            );
            var spec = new ColumnaPuedeSerRenombradaSpecification();
            Assert.That(spec.IsSatisfiedBy(columna), Is.False);
        }
    }
}

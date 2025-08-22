using ListaPreciosBC.Domain.Specifications;
using ListaPreciosBC.Domain.ValueObjects;
using NUnit.Framework;
using System.Collections.Generic;

namespace ListaPreciosBC.Tests.Domain.Tests.BusinessRulesTests.SpecificationsTests
{
    [TestFixture]
    public class PlantillaColumnasUnicasSpecificationTests
    {
        [Test]
        public void IsSatisfiedBy_ReturnsTrue_WhenAllIdsAreUnique()
        {
            var columnas = new List<ConfiguracionColumnaPrecio>
            {
                ConfiguracionColumnaPrecio.Crear(IdentificadorColumnaPrecio.DesdeNumero(1), NombreColumnaPrecio.Crear("A"), ModoValorizacionColumna.Fijo),
                ConfiguracionColumnaPrecio.Crear(IdentificadorColumnaPrecio.DesdeNumero(2), NombreColumnaPrecio.Crear("B"), ModoValorizacionColumna.Fijo)
            };
            var spec = new PlantillaColumnasUnicasSpecification();
            Assert.That(spec.IsSatisfiedBy(columnas), Is.True);
        }

        [Test]
        public void IsSatisfiedBy_ReturnsFalse_WhenIdsAreNotUnique()
        {
            var columnas = new List<ConfiguracionColumnaPrecio>
            {
                ConfiguracionColumnaPrecio.Crear(IdentificadorColumnaPrecio.DesdeNumero(1), NombreColumnaPrecio.Crear("A"), ModoValorizacionColumna.Fijo),
                ConfiguracionColumnaPrecio.Crear(IdentificadorColumnaPrecio.DesdeNumero(1), NombreColumnaPrecio.Crear("B"), ModoValorizacionColumna.Fijo)
            };
            var spec = new PlantillaColumnasUnicasSpecification();
            Assert.That(spec.IsSatisfiedBy(columnas), Is.False);
        }
    }
}

using ListaPreciosBC.Domain.Specifications;
using ListaPreciosBC.Domain.ValueObjects;
using NUnit.Framework;
using System.Collections.Generic;

namespace ListaPreciosBC.Tests.Domain.Tests.BusinessRulesTests.SpecificationsTests
{
    [TestFixture]
    public class PlantillaTieneColumnaVisibleSpecificationTests
    {
        [Test]
        public void IsSatisfiedBy_ReturnsTrue_WhenAtLeastOneColumnaIsVisible()
        {
            var columnas = new List<ConfiguracionColumnaPrecio>
            {
                ConfiguracionColumnaPrecio.Crear(IdentificadorColumnaPrecio.DesdeNumero(1), NombreColumnaPrecio.Crear("A"), ModoValorizacionColumna.Fijo, visible: true),
                ConfiguracionColumnaPrecio.Crear(IdentificadorColumnaPrecio.DesdeNumero(2), NombreColumnaPrecio.Crear("B"), ModoValorizacionColumna.Fijo, visible: false)
            };
            var spec = new PlantillaTieneColumnaVisibleSpecification();
            Assert.That(spec.IsSatisfiedBy(columnas), Is.True);
        }

        [Test]
        public void IsSatisfiedBy_ReturnsFalse_WhenNoColumnaIsVisible()
        {
            var columnas = new List<ConfiguracionColumnaPrecio>
            {
                ConfiguracionColumnaPrecio.Crear(IdentificadorColumnaPrecio.DesdeNumero(1), NombreColumnaPrecio.Crear("A"), ModoValorizacionColumna.Fijo, visible: false),
                ConfiguracionColumnaPrecio.Crear(IdentificadorColumnaPrecio.DesdeNumero(2), NombreColumnaPrecio.Crear("B"), ModoValorizacionColumna.Fijo, visible: false)
            };
            var spec = new PlantillaTieneColumnaVisibleSpecification();
            Assert.That(spec.IsSatisfiedBy(columnas), Is.False);
        }
    }
}

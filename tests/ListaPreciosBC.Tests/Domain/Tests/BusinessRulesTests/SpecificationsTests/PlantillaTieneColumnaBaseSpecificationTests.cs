using ListaPreciosBC.Domain.Specifications;
using ListaPreciosBC.Domain.ValueObjects;
using NUnit.Framework;
using System.Collections.Generic;

namespace ListaPreciosBC.Tests.Domain.Tests.BusinessRulesTests.SpecificationsTests
{
    [TestFixture]
    public class PlantillaTieneColumnaBaseSpecificationTests
    {
        [Test]
        public void IsSatisfiedBy_ReturnsTrue_WhenAtLeastOneColumnaIsBase()
        {
            var columnas = new List<ConfiguracionColumnaPrecio>
            {
                ConfiguracionColumnaPrecio.Crear(IdentificadorColumnaPrecio.DesdeNumero(1), NombreColumnaPrecio.Crear("A"), ModoValorizacionColumna.Fijo, esBase: true),
                ConfiguracionColumnaPrecio.Crear(IdentificadorColumnaPrecio.DesdeNumero(2), NombreColumnaPrecio.Crear("B"), ModoValorizacionColumna.Fijo, esBase: false)
            };
            var spec = new PlantillaTieneColumnaBaseSpecification();
            Assert.That(spec.IsSatisfiedBy(columnas), Is.True);
        }

        [Test]
        public void IsSatisfiedBy_ReturnsFalse_WhenNoColumnaIsBase()
        {
            var columnas = new List<ConfiguracionColumnaPrecio>
            {
                ConfiguracionColumnaPrecio.Crear(IdentificadorColumnaPrecio.DesdeNumero(1), NombreColumnaPrecio.Crear("A"), ModoValorizacionColumna.Fijo, esBase: false),
                ConfiguracionColumnaPrecio.Crear(IdentificadorColumnaPrecio.DesdeNumero(2), NombreColumnaPrecio.Crear("B"), ModoValorizacionColumna.Fijo, esBase: false)
            };
            var spec = new PlantillaTieneColumnaBaseSpecification();
            Assert.That(spec.IsSatisfiedBy(columnas), Is.False);
        }
    }
}

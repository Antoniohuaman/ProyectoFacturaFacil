using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ListaPreciosBC.Domain.Policies;
using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Tests.Domain.Tests.BusinessRulesTests.PoliciesTests
{
    [TestFixture]
    public class PuedeAgregarColumnaPolicyTests
    {
        [Test]
        public void Validar_DeberiaRetornarTrue_CuandoHayMenosDeMaxOrden()
        {
                var columnas = Enumerable.Range(1, ConfiguracionColumnaPrecio.MaxOrden - 1)
                    .Select(i => ConfiguracionColumnaPrecio.Crear(
                        IdentificadorColumnaPrecio.DesdeNumero((byte)i),
                        NombreColumnaPrecio.Crear($"Columna{i}"),
                        ModoValorizacionColumna.Fijo
                    )).ToList();
            var resultado = PuedeAgregarColumnaPolicy.Validar(columnas);
            Assert.That(resultado, Is.True);
        }

        [Test]
        public void Validar_DeberiaRetornarFalse_CuandoHayMaxOrden()
        {
                var columnas = Enumerable.Range(1, ConfiguracionColumnaPrecio.MaxOrden)
                    .Select(i => ConfiguracionColumnaPrecio.Crear(
                        IdentificadorColumnaPrecio.DesdeNumero((byte)i),
                        NombreColumnaPrecio.Crear($"Columna{i}"),
                        ModoValorizacionColumna.Fijo
                    )).ToList();
            var resultado = PuedeAgregarColumnaPolicy.Validar(columnas);
            Assert.That(resultado, Is.False);
        }

        [Test]
        public void Validar_DeberiaRetornarTrue_CuandoNoHayColumnas()
        {
            var columnas = new List<ConfiguracionColumnaPrecio>();
            var resultado = PuedeAgregarColumnaPolicy.Validar(columnas);
            Assert.That(resultado, Is.True);
        }
    }
}

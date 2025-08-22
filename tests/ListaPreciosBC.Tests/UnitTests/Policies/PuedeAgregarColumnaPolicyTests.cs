using ListaPreciosBC.Domain.Policies;
using ListaPreciosBC.Domain.ValueObjects;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace ListaPreciosBC.Tests.UnitTests.Policies
{
    [TestFixture]
    public class PuedeAgregarColumnaPolicyTests
    {
        [Test]
        public void Validar_MenosDeMaximoColumnas_RetornaTrue()
        {
            var columnas = Enumerable.Range(1, ConfiguracionColumnaPrecio.MaxOrden - 1)
                .Select(i =>
                {
                    var id = IdentificadorColumnaPrecio.Crear($"P{i}");
                    var nombre = NombreColumnaPrecio.Crear($"Col{i}");
                    var modo = ModoValorizacionColumna.Fijo;
                    return ConfiguracionColumnaPrecio.Crear(id, nombre, modo, esBase: false, visible: true, orden: (byte)i);
                });
            Assert.That(PuedeAgregarColumnaPolicy.Validar(columnas), Is.True);
        }

        [Test]
        public void Validar_ExactamenteMaximoColumnas_RetornaFalse()
        {
            var columnas = Enumerable.Range(1, ConfiguracionColumnaPrecio.MaxOrden)
                .Select(i =>
                {
                    var id = IdentificadorColumnaPrecio.Crear($"P{i}");
                    var nombre = NombreColumnaPrecio.Crear($"Col{i}");
                    var modo = ModoValorizacionColumna.Fijo;
                    return ConfiguracionColumnaPrecio.Crear(id, nombre, modo, esBase: false, visible: true, orden: (byte)i);
                });
            Assert.That(PuedeAgregarColumnaPolicy.Validar(columnas), Is.False);
        }
    }
}

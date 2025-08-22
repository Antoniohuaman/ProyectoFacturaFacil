using ListaPreciosBC.Domain.Policies;
using ListaPreciosBC.Domain.ValueObjects;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace ListaPreciosBC.Tests.UnitTests.Policies
{
    [TestFixture]
    public class PuedeEliminarColumnaPolicyTests
    {
        [Test]
        public void Validar_EliminaColumnaVisibleYBase_RetornaTrue()
        {
            var columnas = new List<ConfiguracionColumnaPrecio>
            {
                ConfiguracionColumnaPrecio.Crear(
                    IdentificadorColumnaPrecio.Crear("P1"),
                    NombreColumnaPrecio.Crear("Base"),
                    ModoValorizacionColumna.Fijo,
                    esBase: true,
                    visible: true,
                    orden: 1
                ),
                ConfiguracionColumnaPrecio.Crear(
                    IdentificadorColumnaPrecio.Crear("P2"),
                    NombreColumnaPrecio.Crear("Col2"),
                    ModoValorizacionColumna.Fijo,
                    esBase: false,
                    visible: true,
                    orden: 2
                )
            };
            var columnaAEliminar = IdentificadorColumnaPrecio.Crear("P2");
            Assert.That(PuedeEliminarColumnaPolicy.Validar(columnaAEliminar, columnas), Is.True);
        }

        [Test]
        public void Validar_EliminaColumnaBaseYNoQuedaOtraBase_RetornaFalse()
        {
            var columnas = new List<ConfiguracionColumnaPrecio>
            {
                ConfiguracionColumnaPrecio.Crear(
                    IdentificadorColumnaPrecio.Crear("P1"),
                    NombreColumnaPrecio.Crear("Base"),
                    ModoValorizacionColumna.Fijo,
                    esBase: true,
                    visible: true,
                    orden: 1
                ),
                ConfiguracionColumnaPrecio.Crear(
                    IdentificadorColumnaPrecio.Crear("P2"),
                    NombreColumnaPrecio.Crear("Col2"),
                    ModoValorizacionColumna.Fijo,
                    esBase: false,
                    visible: true,
                    orden: 2
                )
            };
            var columnaAEliminar = IdentificadorColumnaPrecio.Crear("P1");
            Assert.That(PuedeEliminarColumnaPolicy.Validar(columnaAEliminar, columnas), Is.False);
        }

        [Test]
        public void Validar_EliminaColumnaVisibleYNoQuedaVisible_RetornaFalse()
        {
            var columnas = new List<ConfiguracionColumnaPrecio>
            {
                ConfiguracionColumnaPrecio.Crear(
                    IdentificadorColumnaPrecio.Crear("P1"),
                    NombreColumnaPrecio.Crear("Base"),
                    ModoValorizacionColumna.Fijo,
                    esBase: true,
                    visible: false,
                    orden: 1
                ),
                ConfiguracionColumnaPrecio.Crear(
                    IdentificadorColumnaPrecio.Crear("P2"),
                    NombreColumnaPrecio.Crear("Col2"),
                    ModoValorizacionColumna.Fijo,
                    esBase: false,
                    visible: true,
                    orden: 2
                )
            };
            var columnaAEliminar = IdentificadorColumnaPrecio.Crear("P2");
            Assert.That(PuedeEliminarColumnaPolicy.Validar(columnaAEliminar, columnas), Is.False);
        }
    }
}

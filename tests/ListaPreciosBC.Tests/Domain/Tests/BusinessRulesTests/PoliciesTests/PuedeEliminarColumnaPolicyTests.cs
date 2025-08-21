using NUnit.Framework;
using ListaPreciosBC.Domain.Policies;
using ListaPreciosBC.Domain.ValueObjects;
using System.Collections.Generic;

namespace ListaPreciosBC.Tests.Domain.Tests.BusinessRulesTests.PoliciesTests
{
    [TestFixture]
    public class PuedeEliminarColumnaPolicyTests
    {
        private ConfiguracionColumnaPrecio CrearColumna(string id, string nombre, string modo, bool esBase, bool visible, byte orden)
        {
            var idVO = IdentificadorColumnaPrecio.Crear(id);
            var nombreVO = NombreColumnaPrecio.Crear(nombre);
            var modoVO = ModoValorizacionColumna.Crear(modo);
            return ConfiguracionColumnaPrecio.Crear(idVO, nombreVO, modoVO, esBase, visible, orden);
        }

        [Test]
        public void Validar_DeberiaRetornarFalse_SiSoloQuedaUnaColumna()
        {
            var columna1 = CrearColumna("P1", "Base", "Fijo", true, true, 1);
            var columnas = new List<ConfiguracionColumnaPrecio> { columna1 };
            var resultado = PuedeEliminarColumnaPolicy.Validar(columna1.Id, columnas);
            Assert.That(resultado, Is.False);
        }

        [Test]
        public void Validar_DeberiaRetornarFalse_SiNoQuedaColumnaVisible()
        {
            var columna1 = CrearColumna("P1", "Base", "Fijo", true, false, 1);
            var columna2 = CrearColumna("P2", "Secundaria", "Fijo", false, true, 2);
            var columnas = new List<ConfiguracionColumnaPrecio> { columna1, columna2 };
            var resultado = PuedeEliminarColumnaPolicy.Validar(columna2.Id, columnas);
            Assert.That(resultado, Is.False);
        }

        [Test]
        public void Validar_DeberiaRetornarFalse_SiNoQuedaColumnaBase()
        {
            var columna1 = CrearColumna("P1", "Base", "Fijo", true, true, 1);
            var columna2 = CrearColumna("P2", "Secundaria", "Fijo", false, true, 2);
            var columnas = new List<ConfiguracionColumnaPrecio> { columna1, columna2 };
            var resultado = PuedeEliminarColumnaPolicy.Validar(columna1.Id, columnas);
            Assert.That(resultado, Is.False);
        }

        [Test]
        public void Validar_DeberiaRetornarTrue_SiQuedaColumnaVisibleYBase()
        {
            var columna1 = CrearColumna("P1", "Base", "Fijo", true, true, 1);
            var columna2 = CrearColumna("P2", "Secundaria", "Fijo", false, true, 2);
            var columnas = new List<ConfiguracionColumnaPrecio> { columna1, columna2 };
            var resultado = PuedeEliminarColumnaPolicy.Validar(columna2.Id, columnas);
            Assert.That(resultado, Is.True);
        }

        [Test]
        public void Validar_DeberiaRetornarTrue_SiQuedanVariasVisiblesYBase()
        {
            var columna1 = CrearColumna("P1", "Base", "Fijo", true, true, 1);
            var columna2 = CrearColumna("P2", "Secundaria", "Fijo", false, true, 2);
            var columna3 = CrearColumna("P3", "Extra", "Fijo", false, true, 3);
            var columnas = new List<ConfiguracionColumnaPrecio> { columna1, columna2, columna3 };
            var resultado = PuedeEliminarColumnaPolicy.Validar(columna2.Id, columnas);
            Assert.That(resultado, Is.True);
        }
    }
}

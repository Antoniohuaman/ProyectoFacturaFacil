using NUnit.Framework;
using ListaPreciosBC.Domain.Policies;
using ListaPreciosBC.Domain.ValueObjects;
using System.Collections.Generic;

namespace ListaPreciosBC.Tests.Domain.Tests.BusinessRulesTests.PoliciesTests
{
    [TestFixture]
    public class PuedeSerColumnaBasePolicyTests
    {
        private ConfiguracionColumnaPrecio CrearColumna(string id, string nombre, string modo, bool esBase, bool visible, byte orden)
        {
            var idVO = IdentificadorColumnaPrecio.Crear(id);
            var nombreVO = NombreColumnaPrecio.Crear(nombre);
            var modoVO = ModoValorizacionColumna.Crear(modo);
            return ConfiguracionColumnaPrecio.Crear(
                idVO,
                nombreVO,
                modoVO,
                esBase: esBase,
                visible: visible,
                orden: orden);
        }

        [Test]
        public void Validar_DeberiaRetornarTrue_SiColumnaEsVisible_Fijo_SinOtraBase()
        {
            var columna = CrearColumna("P1", "Base", "Fijo", false, true, 1);
            var columnas = new List<ConfiguracionColumnaPrecio> { columna };
            var resultado = PuedeSerColumnaBasePolicy.Validar(columna, columnas);
            Assert.That(resultado, Is.True);
        }

        [Test]
        public void Validar_DeberiaRetornarFalse_SiColumnaNoEsVisible()
        {
            var columna = CrearColumna("P1", "Base", "Fijo", false, false, 1);
            var columnas = new List<ConfiguracionColumnaPrecio> { columna };
            var resultado = PuedeSerColumnaBasePolicy.Validar(columna, columnas);
            Assert.That(resultado, Is.False);
        }

        [Test]
        public void Validar_DeberiaRetornarFalse_SiColumnaNoEsFijo()
        {
            var columna = CrearColumna("P1", "Base", "PorVolumen", false, true, 1);
            var columnas = new List<ConfiguracionColumnaPrecio> { columna };
            var resultado = PuedeSerColumnaBasePolicy.Validar(columna, columnas);
            Assert.That(resultado, Is.False);
        }

        [Test]
        public void Validar_DeberiaRetornarFalse_SiYaHayOtraColumnaBase()
        {
            var columna1 = CrearColumna("P1", "Base", "Fijo", false, true, 1);
            var columna2 = CrearColumna("P2", "Secundaria", "Fijo", true, true, 2);
            var columnas = new List<ConfiguracionColumnaPrecio> { columna1, columna2 };
            var resultado = PuedeSerColumnaBasePolicy.Validar(columna1, columnas);
            Assert.That(resultado, Is.False);
        }

        [Test]
        public void Validar_DeberiaRetornarTrue_SiSoloEstaMarcadaComoBaseEllaMisma()
        {
            var columna = CrearColumna("P1", "Base", "Fijo", true, true, 1);
            var columnas = new List<ConfiguracionColumnaPrecio> { columna };
            var resultado = PuedeSerColumnaBasePolicy.Validar(columna, columnas);
            Assert.That(resultado, Is.True);
        }
    }
}

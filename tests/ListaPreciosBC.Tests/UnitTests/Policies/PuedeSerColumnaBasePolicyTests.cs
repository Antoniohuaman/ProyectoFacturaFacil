using ListaPreciosBC.Domain.Policies;
using ListaPreciosBC.Domain.ValueObjects;
using NUnit.Framework;
using System.Collections.Generic;

namespace ListaPreciosBC.Tests.UnitTests.Policies
{
    [TestFixture]
    public class PuedeSerColumnaBasePolicyTests
    {
        [Test]
        public void Validar_ColumnaVisibleModoFijoSinOtraBase_RetornaTrue()
        {
            var columna = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.Crear("P1"),
                NombreColumnaPrecio.Crear("Base"),
                ModoValorizacionColumna.Fijo,
                esBase: false,
                visible: true,
                orden: 1
            );
            var todas = new List<ConfiguracionColumnaPrecio> { columna };
            Assert.That(PuedeSerColumnaBasePolicy.Validar(columna, todas), Is.True);
        }

        [Test]
        public void Validar_ColumnaNoVisible_RetornaFalse()
        {
            var columna = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.Crear("P1"),
                NombreColumnaPrecio.Crear("Base"),
                ModoValorizacionColumna.Fijo,
                esBase: false,
                visible: false,
                orden: 1
            );
            var todas = new List<ConfiguracionColumnaPrecio> { columna };
            Assert.That(PuedeSerColumnaBasePolicy.Validar(columna, todas), Is.False);
        }

        [Test]
        public void Validar_ColumnaModoNoFijo_RetornaFalse()
        {
            var columna = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.Crear("P1"),
                NombreColumnaPrecio.Crear("Base"),
                ModoValorizacionColumna.PorVolumen,
                esBase: false,
                visible: true,
                orden: 1
            );
            var todas = new List<ConfiguracionColumnaPrecio> { columna };
            Assert.That(PuedeSerColumnaBasePolicy.Validar(columna, todas), Is.False);
        }

        [Test]
        public void Validar_OtraColumnaYaEsBase_RetornaFalse()
        {
            var columna = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.Crear("P1"),
                NombreColumnaPrecio.Crear("Base"),
                ModoValorizacionColumna.Fijo,
                esBase: false,
                visible: true,
                orden: 1
            );
            var otraBase = ConfiguracionColumnaPrecio.Crear(
                IdentificadorColumnaPrecio.Crear("P2"),
                NombreColumnaPrecio.Crear("OtraBase"),
                ModoValorizacionColumna.Fijo,
                esBase: true,
                visible: true,
                orden: 2
            );
            var todas = new List<ConfiguracionColumnaPrecio> { columna, otraBase };
            Assert.That(PuedeSerColumnaBasePolicy.Validar(columna, todas), Is.False);
        }
    }
}

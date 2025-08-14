using System;
using NUnit.Framework;
// Necesario para StringAssert
using NUnit.Framework;
using SharedKernel.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.UnitTests.ValueObjects
{
    [TestFixture]
    public class CentroDeCostoTests
    {
        // -------- Helpers
        private static string Repeat(char c, int n) => new string(c, n);

        // -------- Casos felices
        [Test]
        public void Create_SoloCodigo_Valido_NormalizaTrimYMayusculas()
        {
            var cc = CentroDeCosto.Create("  cc-ventas / 01  ", "Ventas");

            Assert.Multiple(() =>
            {
                Assert.That(cc.Code, Is.EqualTo("CC-VENTAS / 01")); // upper + trim
                Assert.That(cc.Name, Is.EqualTo("Ventas"));
                Assert.That(cc.ToString(), Is.EqualTo("CC-VENTAS / 01 - Ventas"));
                Assert.That(cc.ForUbl_AccountingCostCode(), Is.EqualTo("CC-VENTAS / 01"));
                Assert.That(cc.ForUbl_AccountingCost(), Is.EqualTo("Ventas")); // con nombre → usa Name
            });
        }

        [Test]
        public void Create_CodigoYNombre_Valido_RespetaLimitesYTrim()
        {
            var cc = CentroDeCosto.Create(" ALM-01 ", "  Almacén Central  ");

            Assert.Multiple(() =>
            {
                Assert.That(cc.Code, Is.EqualTo("ALM-01"));
                Assert.That(cc.Name, Is.EqualTo("Almacén Central"));
                Assert.That(cc.ToString(), Is.EqualTo("ALM-01 - Almacén Central"));
                Assert.That(cc.ForUbl_AccountingCost(), Is.EqualTo("Almacén Central"));
            });
        }

        [Test]
        public void Create_AceptaLongitudMaximaDeCodigo_35()
        {
            var code35 = Repeat('A', 35);
            var cc = CentroDeCosto.Create(code35, "Nombre");

            Assert.That(cc.Code.Length, Is.EqualTo(35));
        }

        [Test]
        public void Create_AceptaNombreHasta100()
        {
            var name100 = Repeat('B', 100);
            var cc = CentroDeCosto.Create("COD", name100);

            Assert.That(cc.Name!.Length, Is.EqualTo(100));
        }

        // -------- Validaciones
        [Test]
        public void Create_Falla_SiCodigoVacioOWhitespace()
        {
            Assert.That(() => CentroDeCosto.Create(" "), Throws.ArgumentException);
        }

        [Test]
        public void Create_Falla_SiCodigoExcede35()
        {
            var code36 = Repeat('X', 36);
            var ex = Assert.Throws<ArgumentException>(() => CentroDeCosto.Create(code36, "Nombre"));
            Assert.That(ex!.Message, Does.Contain("exceder 35"));
        }

        [TestCase("ABC,123")] // coma no permitida
        [TestCase("ABCÑ")]    // ñ no permitida (solo A-Z ASCII)
        [TestCase("ABC@123")] // @ no permitido
        [TestCase("A|B")]     // | no permitido
        public void Create_Falla_SiCodigoTieneCaracteresNoPermitidos(string bad)
        {
            Assert.That(() => CentroDeCosto.Create(bad), Throws.ArgumentException);
        }

        [Test]
        public void Create_Falla_SiNombreExcede100()
        {
            var name101 = Repeat('N', 101);
            var ex = Assert.Throws<ArgumentException>(() => CentroDeCosto.Create("COD", name101));
            Assert.That(ex!.Message, Does.Contain("exceder 100"));
        }

        [Test]
        public void Create_NombreWhitespace_SeNormalizaANull()
        {
            var ex = Assert.Throws<ArgumentException>(() => CentroDeCosto.Create("COD", "   "));
            Assert.That(ex!.Message, Does.Contain("obligatoria"));
        }

        // -------- FromOptional
        [Test]
        public void FromOptional_NullOVacio_RetornaNull()
        {
            Assert.That(CentroDeCosto.FromOptional(null), Is.Null);
            Assert.That(CentroDeCosto.FromOptional("   "), Is.Null);
        }

        [Test]
        public void FromOptional_ConCodigo_RetornaInstanciaNormalizada()
        {
            var opt = CentroDeCosto.FromOptional("  dep-01  ", "  Depósito  ");
            Assert.That(opt, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(opt!.Value.Code, Is.EqualTo("DEP-01"));
                Assert.That(opt!.Value.Name, Is.EqualTo("Depósito"));
            });
        }

        // -------- Igualdad por valor
        [Test]
        public void Equality_MismoCodigoYNombre_SonIguales()
        {
            var a = CentroDeCosto.Create("ALM-01", "Almacén");
            var b = CentroDeCosto.Create("alm-01", "  Almacén  "); // normaliza a lo mismo

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }
    }
}

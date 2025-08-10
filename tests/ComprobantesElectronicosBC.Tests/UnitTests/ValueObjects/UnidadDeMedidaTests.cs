using System;
using NUnit.Framework;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.UnitTests.ValueObjects
{
    public class UnidadDeMedidaTests
    {
        [Test]
        public void Create_Valido_NormalizaYGuardaCampos()
        {
            var um = UnidadDeMedida.Create("  niu  ", "  Unidad  ");
            Assert.Multiple(() =>
            {
                Assert.That(um.Codigo, Is.EqualTo("NIU"));
                Assert.That(um.Nombre, Is.EqualTo("Unidad"));
                Assert.That(um.UblUnitCode, Is.EqualTo("NIU"));
                Assert.That(UnidadDeMedida.UblUnitCodeListID, Is.EqualTo("UN/ECE rec 20"));
                Assert.That(UnidadDeMedida.UblUnitCodeListAgencyName, Is.EqualTo("United Nations Economic Commission for Europe"));
                Assert.That(um.ToString(), Is.EqualTo("NIU (Unidad)"));
            });
        }

        [Test]
        public void Create_Valido_SinNombre_UsaSoloCodigo()
        {
            var um = UnidadDeMedida.Create("E48");
            Assert.Multiple(() =>
            {
                Assert.That(um.Codigo, Is.EqualTo("E48"));
                Assert.That(um.Nombre, Is.Null);
                Assert.That(um.ToString(), Is.EqualTo("E48"));
            });
        }

        [Test]
        public void Create_CodigoInvalido_Lanza()
        {
            // menos de 2, más de 3, o con caracteres no alfanuméricos
            Assert.Multiple(() =>
            {
                Assert.That(() => UnidadDeMedida.Create("N"), Throws.ArgumentException);
                Assert.That(() => UnidadDeMedida.Create("ABCD"), Throws.ArgumentException);
                Assert.That(() => UnidadDeMedida.Create("N-U"), Throws.ArgumentException);
                Assert.That(() => UnidadDeMedida.Create("ÑU"), Throws.ArgumentException);
                Assert.That(() => UnidadDeMedida.Create(" "), Throws.ArgumentException);
            });
        }

        [Test]
        public void Create_NombreMuyLargo_Lanza()
        {
            var nombreLargo = new string('X', 61);
            Assert.That(() => UnidadDeMedida.Create("NIU", nombreLargo), Throws.ArgumentException);
        }

        [Test]
        public void TryCreate_Invalido_NoLanzaYDevuelveFalse()
        {
            var ok = UnidadDeMedida.TryCreate("ABCD", "x", out var um);
            Assert.Multiple(() =>
            {
                Assert.That(ok, Is.False);
                Assert.That(um, Is.Null);
            });
        }

        [Test]
        public void Atajos_Comunes_DevuelvenCodigosEsperados()
        {
            var niu = UnidadDeMedida.NIU();           // por defecto "Unidad"
            var e48 = UnidadDeMedida.E48("Servicio"); // nombre personalizado
            var kgm = UnidadDeMedida.KGM();
            var ltr = UnidadDeMedida.LTR();
            var mtr = UnidadDeMedida.MTR();
            var zz  = UnidadDeMedida.ZZ();

            Assert.Multiple(() =>
            {
                Assert.That(niu.Codigo, Is.EqualTo("NIU"));
                Assert.That(niu.Nombre, Is.EqualTo("Unidad"));

                Assert.That(e48.Codigo, Is.EqualTo("E48"));
                Assert.That(e48.Nombre, Is.EqualTo("Servicio"));

                Assert.That(kgm.Codigo, Is.EqualTo("KGM"));
                Assert.That(ltr.Codigo, Is.EqualTo("LTR"));
                Assert.That(mtr.Codigo, Is.EqualTo("MTR"));
                Assert.That(zz.Codigo,  Is.EqualTo("ZZ"));
            });
        }

        [Test]
        public void ToString_FormateaSegunNombre()
        {
            var conNombre = UnidadDeMedida.Create("KGM", "Kilogramo");
            var sinNombre = UnidadDeMedida.Create("KGM", null);

            Assert.Multiple(() =>
            {
                Assert.That(conNombre.ToString(), Is.EqualTo("KGM (Kilogramo)"));
                Assert.That(sinNombre.ToString(), Is.EqualTo("KGM"));
            });
        }

        [Test]
        public void Equality_MismoValor_EsIgual()
        {
            var a = UnidadDeMedida.Create("niu", "Unidad");
            var b = UnidadDeMedida.Create("NIU", "Unidad");
            var c = UnidadDeMedida.Create("NIU", "Unidad Diferente");

            Assert.Multiple(() =>
            {
                Assert.That(a, Is.EqualTo(b));   // mismo código/nombre
                Assert.That(a, Is.Not.EqualTo(c)); // cambia el nombre, cambia el valor
            });
        }
    }
}

using System;
using NUnit.Framework;
using SharedKernel.ValueObjects;

namespace SharedKernel.Tests.UnitTests.ValueObjects
{
    [TestFixture]
    public class AfectacionImpuestoTests
    {
        // -------------------- CASOS VÁLIDOS (Cat. 07) --------------------

        [TestCase("10", AfectacionImpuesto.CategoriaAfectacion.Gravado,    "1000", "IGV",  true,  false)]
        [TestCase("11", AfectacionImpuesto.CategoriaAfectacion.Gravado,    "1000", "IGV",  true,  false)]
        [TestCase("12", AfectacionImpuesto.CategoriaAfectacion.Gravado,    "1000", "IGV",  true,  false)]
        [TestCase("13", AfectacionImpuesto.CategoriaAfectacion.Gravado,    "1000", "IGV",  true,  false)]
        [TestCase("14", AfectacionImpuesto.CategoriaAfectacion.Gravado,    "1000", "IGV",  true,  false)]
        [TestCase("15", AfectacionImpuesto.CategoriaAfectacion.Gravado,    "1000", "IGV",  true,  false)]
        [TestCase("16", AfectacionImpuesto.CategoriaAfectacion.Gravado,    "1000", "IGV",  true,  false)]

        [TestCase("17", AfectacionImpuesto.CategoriaAfectacion.IVAP,       "1016", "IVAP", true,  false)]

        [TestCase("20", AfectacionImpuesto.CategoriaAfectacion.Exonerado,  "9997", "EXO",  false, false)]
        [TestCase("21", AfectacionImpuesto.CategoriaAfectacion.Exonerado,  "9996", "GRA",  false, true )]

        [TestCase("30", AfectacionImpuesto.CategoriaAfectacion.Inafecto,   "9998", "INA",  false, false)]
        [TestCase("31", AfectacionImpuesto.CategoriaAfectacion.Inafecto,   "9998", "INA",  false, false)]
        [TestCase("32", AfectacionImpuesto.CategoriaAfectacion.Inafecto,   "9998", "INA",  false, false)]
        [TestCase("33", AfectacionImpuesto.CategoriaAfectacion.Inafecto,   "9998", "INA",  false, false)]
        [TestCase("34", AfectacionImpuesto.CategoriaAfectacion.Inafecto,   "9998", "INA",  false, false)]
        [TestCase("35", AfectacionImpuesto.CategoriaAfectacion.Inafecto,   "9998", "INA",  false, false)]
        [TestCase("36", AfectacionImpuesto.CategoriaAfectacion.Inafecto,   "9998", "INA",  false, false)]

        [TestCase("40", AfectacionImpuesto.CategoriaAfectacion.Exportacion,"9995", "EXP",  false, false)]
        public void From_Clasifica_Correctamente(
            string codigo,
            AfectacionImpuesto.CategoriaAfectacion categoria,
            string tributoCodigo,
            string tributoNombre,
            bool grava,
            bool gratuita)
        {
            var a = AfectacionImpuesto.From(codigo);

            Assert.Multiple(() =>
            {
                Assert.That(a.Codigo,        Is.EqualTo(codigo));
                Assert.That(a.Categoria,     Is.EqualTo(categoria));
                Assert.That(a.TributoCodigo, Is.EqualTo(tributoCodigo));
                Assert.That(a.TributoNombre, Is.EqualTo(tributoNombre));
                Assert.That(a.GravaImpuesto, Is.EqualTo(grava));
                Assert.That(a.EsGratuita,    Is.EqualTo(gratuita));
                Assert.That(a.ToString(),    Is.EqualTo(codigo));
            });
        }

        // -------------------- ATAJOS ESTÁTICOS --------------------

        [Test]
        public void Atajos_Estan_Bien_Mapeados()
        {
            Assert.That(AfectacionImpuesto.Gravado_10,     Is.EqualTo(AfectacionImpuesto.From("10")));
            Assert.That(AfectacionImpuesto.Exonerado_20,   Is.EqualTo(AfectacionImpuesto.From("20")));
            Assert.That(AfectacionImpuesto.Gratuita_21,    Is.EqualTo(AfectacionImpuesto.From("21")));
            Assert.That(AfectacionImpuesto.Inafecto_30,    Is.EqualTo(AfectacionImpuesto.From("30")));
            Assert.That(AfectacionImpuesto.Exportacion_40, Is.EqualTo(AfectacionImpuesto.From("40")));
            Assert.That(AfectacionImpuesto.IVAP_17,        Is.EqualTo(AfectacionImpuesto.From("17")));
        }

        // -------------------- HELPERS (EsIGV / EsIVAP / EsNoGravado) --------------------

        [Test]
        public void EsIGV_Solo_Para_10_a_16()
        {
            foreach (var c in new[] { "10","11","12","13","14","15","16" })
                Assert.That(AfectacionImpuesto.From(c).EsIGV, Is.True, $"Código {c} debería ser IGV");
        }

        [Test]
        public void EsIVAP_Solo_Para_17()
        {
            Assert.That(AfectacionImpuesto.From("17").EsIVAP, Is.True);
            foreach (var c in new[] { "10","11","12","13","14","15","16","20","21","30","31","32","33","34","35","36","40" })
                Assert.That(AfectacionImpuesto.From(c).EsIVAP, Is.False, $"Código {c} no debería ser IVAP");
        }

        [Test]
        public void EsNoGravado_Para_20_21_30_36_40()
        {
            foreach (var c in new[] { "20","21","30","31","32","33","34","35","36","40" })
                Assert.That(AfectacionImpuesto.From(c).EsNoGravado, Is.True, $"Código {c} debería ser No Gravado");
            foreach (var c in new[] { "10","11","12","13","14","15","16","17" })
                Assert.That(AfectacionImpuesto.From(c).EsNoGravado, Is.False, $"Código {c} no debería ser No Gravado");
        }

        // -------------------- TRYFROM --------------------

        [Test]
        public void TryFrom_Valido_DevuelveTrue_YObjeto()
        {
            var ok = AfectacionImpuesto.TryFrom("10", out var a);
            Assert.Multiple(() =>
            {
                Assert.That(ok, Is.True);
                Assert.That(a,  Is.Not.Null);
                Assert.That(a!.Codigo, Is.EqualTo("10"));
            });
        }

        [Test]
        public void TryFrom_Invalido_DevuelveFalse_YNull()
        {
            var ok = AfectacionImpuesto.TryFrom("18", out var a); // 18 no existe
            Assert.Multiple(() =>
            {
                Assert.That(ok, Is.False);
                Assert.That(a,  Is.Null);
            });
        }

        // -------------------- ERRORES DE FORMATO --------------------

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void From_Lanza_ArgumentException_Si_Null_Vacio_Blanco(string? code)
        {
            Assert.Throws<ArgumentException>(() => AfectacionImpuesto.From(code!));
        }

        [TestCase("1")]
        [TestCase("100")]
        [TestCase("A0")]
        [TestCase("0A")]
        [TestCase("AA")]
        [TestCase("4O")] // O en lugar de 0
        public void From_Lanza_ArgumentException_Si_NoTiene_DosDigitos(string code)
        {
            Assert.Throws<ArgumentException>(() => AfectacionImpuesto.From(code));
        }

        // -------------------- CODIGOS DESCONOCIDOS --------------------

        [TestCase("18")]
        [TestCase("19")]
        [TestCase("22")]
        [TestCase("37")]
        [TestCase("41")]
        [TestCase("99")]
        public void From_Lanza_ArgumentOutOfRange_Si_CodigoNoReconocido(string code)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => AfectacionImpuesto.From(code));
        }

        // -------------------- IGUALDAD POR VALOR --------------------

        [Test]
        public void IgualdadPorValor()
        {
            var a = AfectacionImpuesto.From("10");
            var b = AfectacionImpuesto.From("10");
            var c = AfectacionImpuesto.From("20");

            Assert.Multiple(() =>
            {
                Assert.That(a, Is.EqualTo(b));
                Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
                Assert.That(a, Is.Not.EqualTo(c));
            });
        }
    }
}

using System;
using NUnit.Framework;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.UnitTests.ValueObjects
{
    public class TipoDeComprobanteTests
    {
        [Test]
        [TestCase("01", "Factura", true, false, 3)]
        [TestCase("03", "Boleta",  false, true,  5)]
        public void Create_PorCodigo_PropiedadesBasicas_OK(
            string codigo, string nombreEsperado, bool esFactura, bool esBoleta, int maxRetro)
        {
            var tipo = TipoDeComprobante.Create(codigo);

            Assert.That(tipo.Codigo, Is.EqualTo(codigo));
            Assert.That(tipo.Nombre, Is.EqualTo(nombreEsperado));
            Assert.That(tipo.EsFactura, Is.EqualTo(esFactura));
            Assert.That(tipo.EsBoleta,  Is.EqualTo(esBoleta));
            Assert.That(tipo.MaxDiasRetroactivos, Is.EqualTo(maxRetro));
            // UBL usa el mismo código
            Assert.That(tipo.UblInvoiceTypeCode, Is.EqualTo(codigo));
        }

        [Test]
        [TestCase("Factura", "01")]
        [TestCase("Boleta", "03")]
        [TestCase("factura", "01")] // case-insensitive
        [TestCase("boleta", "03")]
        public void Create_PorNombre_OK(string nombre, string codigoEsperado)
        {
            var tipo = TipoDeComprobante.Create(nombre);
            Assert.That(tipo.Codigo, Is.EqualTo(codigoEsperado));
        }

        [Test]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("99")]
        [TestCase("Nota de crédito")] // aún no soportado
        public void Create_Invalido_LanzaArgumentException(string input)
        {
            Assert.Throws<ArgumentException>(() => TipoDeComprobante.Create(input));
        }

        [Test]
        public void TryCreate_Valido_TrueYAsignaInstancia()
        {
            var ok = TipoDeComprobante.TryCreate("03", out var tipo);
            Assert.That(ok, Is.True);
            Assert.That(tipo, Is.Not.Null);
            Assert.That(tipo!.EsBoleta, Is.True);
        }

        [Test]
        public void TryCreate_Invalido_FalseYTipoNull()
        {
            var ok = TipoDeComprobante.TryCreate("NC", out var tipo);
            Assert.That(ok, Is.False);
            Assert.That(tipo, Is.Null);
        }

        [Test]
        public void CambioEnFormulario_ReemplazarInstancia_SinEfectosColaterales()
        {
            // Usuario empezó en Boleta…
            var tipo = TipoDeComprobante.Create("03");
            Assert.That(tipo.EsBoleta, Is.True);

            // …y decide cambiar a Factura (simplemente reemplaza el VO)
            tipo = TipoDeComprobante.Create("01");
            Assert.That(tipo.EsFactura, Is.True);
            Assert.That(tipo.RequiereRucCliente, Is.True);
        }

        [Test]
        public void ValidarCompatibilidadConSerie_FConFactura_BConBoleta_OK()
        {
            var factura = TipoDeComprobante.Create("01");
            var boleta  = TipoDeComprobante.Create("03");

            // No debe lanzar
            Assert.DoesNotThrow(() => factura.ValidarCompatibilidadConSerie("F001"));
            Assert.DoesNotThrow(() => boleta.ValidarCompatibilidadConSerie("B9"));
        }

        [Test]
        public void ValidarCompatibilidadConSerie_PrefijoIncompatible_LanzaInvalidOperation()
        {
            var factura = TipoDeComprobante.Create("01");
            var boleta  = TipoDeComprobante.Create("03");

            Assert.Throws<ComprobantesElectronicosBC.Domain.Exceptions.ReglaDeNegocioException>(() => boleta.ValidarCompatibilidadConSerie("F123"));
            Assert.Throws<ComprobantesElectronicosBC.Domain.Exceptions.ReglaDeNegocioException>(() => factura.ValidarCompatibilidadConSerie("B001"));
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("     ")]
        public void ValidarCompatibilidadConSerie_Vacia_LanzaArgumentException(string? serie)
        {
            var factura = TipoDeComprobante.Create("01");
            Assert.Throws<ArgumentException>(() => factura.ValidarCompatibilidadConSerie(serie!));
        }

        [Test]
        [TestCase("F-01")] // guion no permitido (solo alfanumérico)
        [TestCase("F*01")]
        [TestCase("F0012")] // > 4 chars
        public void ValidarCompatibilidadConSerie_FormatoInvalido_LanzaArgumentException(string serie)
        {
            var factura = TipoDeComprobante.Create("01");
            Assert.Throws<ArgumentException>(() => factura.ValidarCompatibilidadConSerie(serie));
        }

        [Test]
        [TestCase("F001", "01")]
        [TestCase("B1",   "03")]
        [TestCase("t001", null)] // no infiere si no inicia con F/B
        [TestCase("",     null)]
        [TestCase("   ",  null)]
        public void InferirDesdeSerie_Comportamiento(string serie, string? codigoEsperado)
        {
            var inferred = TipoDeComprobante.InferirDesdeSerie(serie);
            if (codigoEsperado is null)
            {
                Assert.That(inferred, Is.Null);
            }
            else
            {
                Assert.That(inferred, Is.Not.Null);
                Assert.That(inferred!.Codigo, Is.EqualTo(codigoEsperado));
            }
        }

        [Test]
        public void ReglasNormativas_FacturaRequiereRuc_Y_BoletaNo()
        {
            var factura = TipoDeComprobante.Create("Factura");
            var boleta  = TipoDeComprobante.Create("Boleta");

            Assert.That(factura.RequiereRucCliente, Is.True);
            Assert.That(boleta.RequiereRucCliente,  Is.False);
        }

        [Test]
        public void MaxDiasRetroactivos_ValoresEsperados()
        {
            var factura = TipoDeComprobante.Create("01");
            var boleta  = TipoDeComprobante.Create("03");

            Assert.That(factura.MaxDiasRetroactivos, Is.EqualTo(3));
            Assert.That(boleta.MaxDiasRetroactivos,  Is.EqualTo(5));
        }

        [Test]
        public void IgualdadPorValor_Y_HashCode_Consistentes()
        {
            var a = TipoDeComprobante.Create("01");
            var b = TipoDeComprobante.Create("Factura");
            var c = TipoDeComprobante.Create("03");

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            Assert.That(a, Is.Not.EqualTo(c));
        }

        [Test]
        public void ToString_FormatoLegible()
        {
            var t = TipoDeComprobante.Create("03");
            Assert.That(t.ToString(), Does.StartWith("03"));
            Assert.That(t.ToString(), Does.Contain("Boleta"));
        }
    }
}

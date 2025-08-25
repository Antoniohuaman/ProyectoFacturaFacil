using System;
using System.Collections.Generic;
using NUnit.Framework;
using ConfiguracionSistemaBC.Domain.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.UnitTests.ValueObjects
{
    [TestFixture]
    public class TipoComprobanteCodigoTests
    {
        [Test]
        public void InstanciasConocidas_MVP_DebenEstarDefinidasCorrectamente()
        {
            Assert.That(TipoComprobanteCodigo.Factura.Codigo, Is.EqualTo("01"));
            Assert.That(TipoComprobanteCodigo.Factura.SeriePrefijoConvencional, Is.EqualTo('F'));
            Assert.That(TipoComprobanteCodigo.Factura.EsFactura, Is.True);
            Assert.That(TipoComprobanteCodigo.Factura.EsBoleta, Is.False);

            Assert.That(TipoComprobanteCodigo.Boleta.Codigo, Is.EqualTo("03"));
            Assert.That(TipoComprobanteCodigo.Boleta.SeriePrefijoConvencional, Is.EqualTo('B'));
            Assert.That(TipoComprobanteCodigo.Boleta.EsBoleta, Is.True);
            Assert.That(TipoComprobanteCodigo.Boleta.EsFactura, Is.False);

            Assert.That(TipoComprobanteCodigo.All, Has.Member(TipoComprobanteCodigo.Factura));
            Assert.That(TipoComprobanteCodigo.All, Has.Member(TipoComprobanteCodigo.Boleta));
            Assert.That(new HashSet<TipoComprobanteCodigo>(TipoComprobanteCodigo.All).Count, Is.EqualTo(2));
        }

        [Test]
        public void FromCode_Valido_RetornaInstanciaConocida()
        {
            var f = TipoComprobanteCodigo.FromCode("01");
            var b = TipoComprobanteCodigo.FromCode("03");
            Assert.That(f, Is.SameAs(TipoComprobanteCodigo.Factura));
            Assert.That(b, Is.SameAs(TipoComprobanteCodigo.Boleta));
        }

        [Test]
        public void FromCode_Invalido_LanzaExcepcion()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => TipoComprobanteCodigo.FromCode("07"));
            Assert.That(ex!.Message, Does.Contain("no soportado"));
        }

        [TestCase("01", "01")]
        [TestCase("03", "03")]
        [TestCase("factura", "01")]
        [TestCase("Factura", "01")]
        [TestCase("F", "01")]
        [TestCase("boleta", "03")]
        [TestCase("B", "03")]
        [TestCase("BOLETA DE VENTA", "03")]
        public void From_AceptaCodigosYAliases_NormalizaACodigo(string input, string esperadoCodigo)
        {
            var t = TipoComprobanteCodigo.From(input);
            Assert.That(t.Codigo, Is.EqualTo(esperadoCodigo));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("X")]
        [TestCase("99")]
        public void From_ValorNoReconocido_LanzaExcepcion(string? input)
        {
            Assert.That(() => TipoComprobanteCodigo.From(input!), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TryParse_DebeSerRobusto()
        {
            Assert.That(TipoComprobanteCodigo.TryParse("01", out var f1) && f1 == TipoComprobanteCodigo.Factura, Is.True);
            Assert.That(TipoComprobanteCodigo.TryParse("BOLETA", out var b1) && b1 == TipoComprobanteCodigo.Boleta, Is.True);
            Assert.That(TipoComprobanteCodigo.TryParse("f", out var f2) && f2!.EsFactura, Is.True);
            Assert.That(TipoComprobanteCodigo.TryParse("??", out _), Is.False);
            Assert.That(TipoComprobanteCodigo.TryParse(null, out _), Is.False);
        }

        [Test]
        public void IgualdadPorValor_Y_Operadores_DebenFuncionar()
        {
            var a = TipoComprobanteCodigo.From("01");
            var b = TipoComprobanteCodigo.FromCode("01");
            var c = TipoComprobanteCodigo.From("03");

            Assert.That(a.Equals(b), Is.True);
            Assert.That(a == b, Is.True);
            Assert.That(a != b, Is.False);
            Assert.That(a.Equals(c), Is.False);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void ToString_E_ImplícitoAString_DeberianRetornarCodigo()
        {
            var f = TipoComprobanteCodigo.Factura;
            Assert.That(f.ToString(), Is.EqualTo("01"));

            string s = f; // conversión implícita
            Assert.That(s, Is.EqualTo("01"));
        }

        [Test]
        public void ExplicitoDesdeString_DeberiaCrearDesdeAliasOCodigo()
        {
            var f = (TipoComprobanteCodigo)"FACTURA";
            var b = (TipoComprobanteCodigo)"03";

            Assert.That(f, Is.EqualTo(TipoComprobanteCodigo.Factura));
            Assert.That(b, Is.EqualTo(TipoComprobanteCodigo.Boleta));
        }

        [Test]
        public void SerieSigueConvencion_DebeValidarPrefijosUsuales()
        {
            Assert.That(TipoComprobanteCodigo.Factura.SerieSigueConvencion("F001"), Is.True);
            Assert.That(TipoComprobanteCodigo.Factura.SerieSigueConvencion("B001"), Is.False);

            Assert.That(TipoComprobanteCodigo.Boleta.SerieSigueConvencion("B123"), Is.True);
            Assert.That(TipoComprobanteCodigo.Boleta.SerieSigueConvencion("F999"), Is.False);
            Assert.That(TipoComprobanteCodigo.Boleta.SerieSigueConvencion(null), Is.False);
            Assert.That(TipoComprobanteCodigo.Boleta.SerieSigueConvencion("  "), Is.False);
        }
    }
}

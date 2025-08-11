using System;
using System.Collections.Generic;
using NUnit.Framework;
using ConfiguracionSistemaBC.Domain.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.UnitTests.ValueObjects
{
    [TestFixture]
    public class TipoOperacionTests
    {
        [Test]
        public void Default_EsVentaInterna_0101()
        {
            Assert.That(TipoOperacion.Default, Is.SameAs(TipoOperacion.VentaInterna));
            Assert.That((string)TipoOperacion.Default, Is.EqualTo("0101"));
        }

        [Test]
        public void InstanciasConocidas_EstanDefinidasCorrectamente()
        {
            Assert.That(TipoOperacion.VentaInterna.Codigo, Is.EqualTo("0101"));
            Assert.That(TipoOperacion.VentaInternaGastosDeduciblesPN.Codigo, Is.EqualTo("0112"));
            Assert.That(TipoOperacion.VentaInternaNRUS.Codigo, Is.EqualTo("0113"));
            Assert.That(TipoOperacion.ExportacionBienes.Codigo, Is.EqualTo("0200"));
            Assert.That(TipoOperacion.VentaNoDomiciliadosNoExport.Codigo, Is.EqualTo("0401"));
            Assert.That(TipoOperacion.DetraccionGeneral.Codigo, Is.EqualTo("1001"));
            Assert.That(TipoOperacion.DetraccionTransporteCarga.Codigo, Is.EqualTo("1004"));

            // All contiene todas las instancias y sin duplicados
            var set = new HashSet<TipoOperacion>(TipoOperacion.All);
            Assert.That(set, Has.Member(TipoOperacion.VentaInterna));
            Assert.That(set, Has.Member(TipoOperacion.DetraccionTransporteCarga));
            Assert.That(set.Count, Is.EqualTo(7));
        }

        [Test]
        public void FromCode_Valido_RetornaInstanciaConocida()
        {
            var t1 = TipoOperacion.FromCode("0101");
            var t2 = TipoOperacion.FromCode("1001");
            Assert.That(t1, Is.SameAs(TipoOperacion.VentaInterna));
            Assert.That(t2, Is.SameAs(TipoOperacion.DetraccionGeneral));
        }

        [Test]
        public void FromCode_Invalido_LanzaExcepcion()
        {
            Assert.That(() => TipoOperacion.FromCode("9999"), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => TipoOperacion.FromCode(""), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => TipoOperacion.FromCode(null!), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void From_AceptaCodigosYOtrosAliasesHumanos()
        {
            Assert.That(TipoOperacion.From("venta interna"), Is.EqualTo(TipoOperacion.VentaInterna));
            Assert.That(TipoOperacion.From("DETRACCIÓN"), Is.EqualTo(TipoOperacion.DetraccionGeneral));
            Assert.That(TipoOperacion.From("EXPORTACION BIENES"), Is.EqualTo(TipoOperacion.ExportacionBienes));
            Assert.That(TipoOperacion.From("NRUS"), Is.EqualTo(TipoOperacion.VentaInternaNRUS));
        }

        [Test]
        public void From_ValorNoReconocido_LanzaExcepcion()
        {
            Assert.That(() => TipoOperacion.From("cualquier cosa"), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => TipoOperacion.From(null!), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TryParse_ComportamientoEsperado()
        {
            Assert.That(TipoOperacion.TryParse("0101", out var a), Is.True);
            Assert.That(a, Is.EqualTo(TipoOperacion.VentaInterna));

            Assert.That(TipoOperacion.TryParse("detraccion", out var b), Is.True);
            Assert.That(b, Is.EqualTo(TipoOperacion.DetraccionGeneral));

            Assert.That(TipoOperacion.TryParse("0401", out var c), Is.True);
            Assert.That(c, Is.EqualTo(TipoOperacion.VentaNoDomiciliadosNoExport));

            Assert.That(TipoOperacion.TryParse("NOEXISTE", out _), Is.False);
            Assert.That(TipoOperacion.TryParse(null, out _), Is.False);
            Assert.That(TipoOperacion.TryParse("", out _), Is.False);
        }

        [Test]
        public void Flags_EsVentaInterna_Y_EsSujetaADetraccion()
        {
            Assert.That(TipoOperacion.VentaInterna.EsVentaInterna, Is.True);
            Assert.That(TipoOperacion.VentaInternaGastosDeduciblesPN.EsVentaInterna, Is.True);
            Assert.That(TipoOperacion.VentaInternaNRUS.EsVentaInterna, Is.True);

            Assert.That(TipoOperacion.ExportacionBienes.EsVentaInterna, Is.False);
            Assert.That(TipoOperacion.DetraccionGeneral.EsVentaInterna, Is.False);

            Assert.That(TipoOperacion.DetraccionGeneral.EsSujetaADetraccion, Is.True);
            Assert.That(TipoOperacion.DetraccionTransporteCarga.EsSujetaADetraccion, Is.True);
            Assert.That(TipoOperacion.VentaInterna.EsSujetaADetraccion, Is.False);
            Assert.That(TipoOperacion.ExportacionBienes.EsSujetaADetraccion, Is.False);
        }

        [Test]
        public void IgualdadPorValor_OperadoresYHashCode()
        {
            var x = TipoOperacion.FromCode("0101");
            var y = TipoOperacion.From("VENTA INTERNA"); // alias al mismo código
            var z = TipoOperacion.FromCode("1001");

            Assert.That(x.Equals(y), Is.True);
            Assert.That(x == y, Is.True);
            Assert.That(x != y, Is.False);
            Assert.That(x.Equals(z), Is.False);
            Assert.That(x.GetHashCode(), Is.EqualTo(y.GetHashCode()));
        }

        [Test]
        public void Conversiones_ToString_ImplícitoYExplícito()
        {
            var t = TipoOperacion.VentaInterna;
            Assert.That(t.ToString(), Is.EqualTo("0101"));

            string code = t; // implícito
            Assert.That(code, Is.EqualTo("0101"));

            var t2 = (TipoOperacion)"DETRACCION"; // explícito desde alias
            Assert.That(t2, Is.EqualTo(TipoOperacion.DetraccionGeneral));
        }
    }
}
using System;
using CatalogoArticulosBC.Domain.ValueObjects;
using NUnit.Framework;

namespace CatalogoArticulosBC.Tests.ValueObjects
{
    [TestFixture]
    public class DescuentoProductoTests
    {
        // ---------- FÁBRICAS: PORCENTAJE ----------

        [Test]
        public void DesdePorcentaje_Correcto_NormalizaADosDecimales()
        {
            var d = DescuentoProducto.DesdePorcentaje(12.345m);

            Assert.That(d.Tipo, Is.EqualTo(DescuentoProducto.Modo.Porcentaje));
            Assert.That(d.EsPorcentaje, Is.True);
            Assert.That(d.EsImporte, Is.False);
            Assert.That(d.EsNinguno, Is.False);
            Assert.That(d.Porcentaje, Is.EqualTo(12.35m)); // redondeo away-from-zero
            Assert.That(d.Importe, Is.Null);
        }

        [Test]
        public void DesdePorcentaje_CeroOLimitesInvalidos_Lanza()
        {
            Assert.That(() => DescuentoProducto.DesdePorcentaje(0m),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => DescuentoProducto.DesdePorcentaje(-0.01m),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => DescuentoProducto.DesdePorcentaje(100.0001m),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TryDesdePorcentaje_DevuelveFalseEnInvalidos()
        {
            var ok = DescuentoProducto.TryDesdePorcentaje(10m, out var d1);
            var bad = DescuentoProducto.TryDesdePorcentaje(0m, out var d2);

            Assert.That(ok, Is.True);
            Assert.That(d1, Is.Not.Null);
            Assert.That(bad, Is.False);
            Assert.That(d2, Is.Null);
        }

        // ---------- FÁBRICAS: IMPORTE ----------

        [Test]
        public void DesdeImporte_Correcto_NormalizaADosDecimales()
        {
            var d = DescuentoProducto.DesdeImporte(10.555m);

            Assert.That(d.Tipo, Is.EqualTo(DescuentoProducto.Modo.Importe));
            Assert.That(d.EsImporte, Is.True);
            Assert.That(d.EsPorcentaje, Is.False);
            Assert.That(d.EsNinguno, Is.False);
            Assert.That(d.Importe, Is.EqualTo(10.56m)); // redondeo away-from-zero
            Assert.That(d.Porcentaje, Is.Null);
        }

        [Test]
        public void DesdeImporte_CeroONegativo_Lanza()
        {
            Assert.That(() => DescuentoProducto.DesdeImporte(0m),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => DescuentoProducto.DesdeImporte(-1m),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TryDesdeImporte_DevuelveFalseEnInvalidos()
        {
            var ok = DescuentoProducto.TryDesdeImporte(3m, out var d1);
            var bad = DescuentoProducto.TryDesdeImporte(0m, out var d2);

            Assert.That(ok, Is.True);
            Assert.That(d1, Is.Not.Null);
            Assert.That(bad, Is.False);
            Assert.That(d2, Is.Null);
        }

        // ---------- NINGUNO ----------

        [Test]
        public void Ninguno_NoTieneValores_YNoAplica()
        {
            var d = DescuentoProducto.Ninguno();

            Assert.That(d.Tipo, Is.EqualTo(DescuentoProducto.Modo.Ninguno));
            Assert.That(d.EsNinguno, Is.True);
            Assert.That(d.Porcentaje, Is.Null);
            Assert.That(d.Importe, Is.Null);
            Assert.That(d.CalcularDescuentoSobre(100m), Is.EqualTo(0m));
            Assert.That(d.AplicarSobre(100m), Is.EqualTo(100m));
        }

        // ---------- CÁLCULOS: CalcularDescuentoSobre ----------

        [Test]
        public void CalcularDescuentoSobre_Porcentaje_RedondeaYNoExcedeBase()
        {
            var d = DescuentoProducto.DesdePorcentaje(7.5m);
            Assert.That(d.CalcularDescuentoSobre(100m), Is.EqualTo(7.50m));

            // 199.99 * 10% = 19.999 -> 20.00 (away-from-zero)
            var d10 = DescuentoProducto.DesdePorcentaje(10m);
            Assert.That(d10.CalcularDescuentoSobre(199.99m), Is.EqualTo(20.00m));

            // 100% de 0.01 debe dar 0.01 exacto y no exceder base
            var d100 = DescuentoProducto.DesdePorcentaje(100m);
            Assert.That(d100.CalcularDescuentoSobre(0.01m), Is.EqualTo(0.01m));
        }

        [Test]
        public void CalcularDescuentoSobre_Importe_RespetaCapPorBase()
        {
            var d = DescuentoProducto.DesdeImporte(15m);

            Assert.That(d.CalcularDescuentoSobre(100m), Is.EqualTo(15.00m));
            Assert.That(d.CalcularDescuentoSobre(10m),  Is.EqualTo(10.00m)); // cap por base
            Assert.That(d.CalcularDescuentoSobre(0m),   Is.EqualTo(0.00m));  // base 0 => 0
        }

        [Test]
        public void CalcularDescuentoSobre_BaseNoPositiva_RetornaCero()
        {
            var p = DescuentoProducto.DesdePorcentaje(10m);
            var i = DescuentoProducto.DesdeImporte(5m);

            Assert.That(p.CalcularDescuentoSobre(0m), Is.EqualTo(0m));
            Assert.That(i.CalcularDescuentoSobre(-1m), Is.EqualTo(0m));
        }

        // ---------- CÁLCULOS: AplicarSobre ----------

        [Test]
        public void AplicarSobre_Porcentaje_CalculaPrecioFinal()
        {
            var d = DescuentoProducto.DesdePorcentaje(25m); // 25 de 200 => 50
            Assert.That(d.AplicarSobre(200m), Is.EqualTo(150.00m));
        }

        [Test]
        public void AplicarSobre_Importe_NoPermiteQuedarNegativo()
        {
            var d = DescuentoProducto.DesdeImporte(120m);

            Assert.That(d.AplicarSobre(200m), Is.EqualTo(80.00m));
            Assert.That(d.AplicarSobre(100m), Is.EqualTo(0.00m)); // cap a cero
        }

        // ---------- IGUALDAD / HASH ----------

        [Test]
        public void Igualdad_MismoTipoYMismoValor_SonIguales()
        {
            var a = DescuentoProducto.DesdePorcentaje(10m);
            var b = DescuentoProducto.DesdePorcentaje(10.00m);

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            Assert.That(a == b, Is.True);
            Assert.That(a != b, Is.False);
        }

        [Test]
        public void Igualdad_TipoODatoDistinto_NoSonIguales()
        {
            var p10 = DescuentoProducto.DesdePorcentaje(10m);
            var p15 = DescuentoProducto.DesdePorcentaje(15m);
            var i10 = DescuentoProducto.DesdeImporte(10m);

            Assert.That(p10, Is.Not.EqualTo(p15));
            Assert.That(p10, Is.Not.EqualTo(i10));
            Assert.That(p10 == i10, Is.False);
            Assert.That(p10 != i10, Is.True);
        }

        // ---------- ToString (flex: solo validamos rasgos) ----------

        [Test]
        public void ToString_FormatoAmigable()
        {
            var p = DescuentoProducto.DesdePorcentaje(12.5m);
            var i = DescuentoProducto.DesdeImporte(8m);
            var n = DescuentoProducto.Ninguno();

            Assert.That(p.ToString(), Does.Contain("%"));
            // Puede usar el signo "−" unicode o "-" normal según fuente; verificamos cualquiera
            Assert.That(i.ToString(), Does.Contain("8"));
            Assert.That(n.ToString().ToLowerInvariant(), Does.Contain("descuento"));
        }
    }
}

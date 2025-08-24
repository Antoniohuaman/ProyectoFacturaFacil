using System;
using NUnit.Framework;
using CatalogoArticulosBC.Domain.ValueObjects;

namespace CatalogoArticulosBC.Domain.Tests.ValueObjects
{
    [TestFixture]
    public class DescuentoProductoTests
    {
        // -------------------- Fábricas e invariantes --------------------

        [Test]
        public void Ninguno_TipoCorrecto_SinValores_YSinEfecto()
        {
            var d = DescuentoProducto.Ninguno();

            Assert.That(d.Tipo, Is.EqualTo(DescuentoProducto.Modo.Ninguno));
            Assert.That(d.Porcentaje, Is.Null);
            Assert.That(d.Importe, Is.Null);
            Assert.That(d.EsNinguno, Is.True);
            Assert.That(d.EsPorcentaje, Is.False);
            Assert.That(d.EsImporte, Is.False);

            Assert.That(d.CalcularDescuentoSobre(100m), Is.EqualTo(0m));
            Assert.That(d.AplicarSobre(100m), Is.EqualTo(100m));
            Assert.That(d.ToString(), Is.EqualTo("Sin descuento"));
        }

        [Test]
        public void DesdePorcentaje_Valido_RedondeaA2Decimales_YSeteaModo()
        {
            var d = DescuentoProducto.DesdePorcentaje(12.345m);

            Assert.That(d.Tipo, Is.EqualTo(DescuentoProducto.Modo.Porcentaje));
            Assert.That(d.EsPorcentaje, Is.True);
            Assert.That(d.Porcentaje, Is.EqualTo(12.35m)); // AwayFromZero
            Assert.That(d.Importe, Is.Null);
            Assert.That(d.ToString(), Is.EqualTo("12.35 %"));
        }

        [TestCase(100.0)]         // límite superior permitido
        [TestCase(1.0)]        // ejemplo válido
        [TestCase(50.0)]       // ejemplo válido
        public void DesdePorcentaje_RangoPermitido_NoLanza(double p)
        {
            Assert.That(() => _ = DescuentoProducto.DesdePorcentaje((decimal)p), Throws.Nothing);
        }


        [Test]
        public void DesdeImporte_Valido_RedondeaA2Decimales_YSeteaModo()
        {
            var d = DescuentoProducto.DesdeImporte(1.005m);

            Assert.That(d.Tipo, Is.EqualTo(DescuentoProducto.Modo.Importe));
            Assert.That(d.EsImporte, Is.True);
            Assert.That(d.Importe, Is.EqualTo(1.01m)); // AwayFromZero
            Assert.That(d.Porcentaje, Is.Null);
            Assert.That(d.ToString(), Is.EqualTo("− 1.01"));
        }

        [TestCase(0.0)]
        [TestCase(-5.0)]
        [TestCase(101.0)]
        public void DesdePorcentaje_FueraDeRango_LanzaOutOfRange(double p)
        {
            TestDelegate act = () => _ = DescuentoProducto.DesdePorcentaje((decimal)p);
            Assert.That(act, Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("porcentaje"));
        }

        // -------------------- Try factories --------------------

        [Test]
        public void TryDesdePorcentaje_Valido_TrueYObjNoNulo()
        {
            var ok = DescuentoProducto.TryDesdePorcentaje(15.123m, out var d);

            Assert.That(ok, Is.True);
            Assert.That(d, Is.Not.Null);
            Assert.That(d!.Porcentaje, Is.EqualTo(15.12m));
        }

        [Test]
        public void TryDesdePorcentaje_Invalido_FalseYNull()
        {
            var ok = DescuentoProducto.TryDesdePorcentaje(0m, out var d);

            Assert.That(ok, Is.False);
            Assert.That(d, Is.Null);
        }

        [Test]
        public void TryDesdeImporte_Valido_TrueYObjNoNulo()
        {
            var ok = DescuentoProducto.TryDesdeImporte(2.675m, out var d);

            Assert.That(ok, Is.True);
            Assert.That(d, Is.Not.Null);
            Assert.That(d!.Importe, Is.EqualTo(2.68m)); // AwayFromZero
        }

        [Test]
        public void TryDesdeImporte_Invalido_FalseYNull()
        {
            var ok = DescuentoProducto.TryDesdeImporte(0m, out var d);

            Assert.That(ok, Is.False);
            Assert.That(d, Is.Null);
        }

        // -------------------- Cálculo de descuento --------------------

        [Test]
        public void CalcularDescuentoSobre_Porcentaje_AplicaRedondeo_2Decimales()
        {
            var d = DescuentoProducto.DesdePorcentaje(33.335m); // -> 33.34
            var basePrecio = 0.05m;

            var descuento = d.CalcularDescuentoSobre(basePrecio);

            // 0.05 * 0.3334 = 0.01667 -> 0.02 (AwayFromZero)
            Assert.That(descuento, Is.EqualTo(0.02m));
        }

        [Test]
        public void CalcularDescuentoSobre_Porcentaje_NoExcedeBase()
        {
            var d = DescuentoProducto.DesdePorcentaje(100m);

            Assert.That(d.CalcularDescuentoSobre(10m), Is.EqualTo(10m));
        }

        [Test]
        public void CalcularDescuentoSobre_Importe_CapAlPrecioBase()
        {
            var d = DescuentoProducto.DesdeImporte(50m);

            Assert.That(d.CalcularDescuentoSobre(30m), Is.EqualTo(30m)); // cap
        }

        [TestCase(0)]
        [TestCase(-10)]
        public void CalcularDescuentoSobre_BaseNoPositiva_SiempreCero(decimal basePrecio)
        {
            var p = DescuentoProducto.DesdePorcentaje(10m);
            var i = DescuentoProducto.DesdeImporte(10m);
            var n = DescuentoProducto.Ninguno();

            Assert.That(p.CalcularDescuentoSobre(basePrecio), Is.EqualTo(0m));
            Assert.That(i.CalcularDescuentoSobre(basePrecio), Is.EqualTo(0m));
            Assert.That(n.CalcularDescuentoSobre(basePrecio), Is.EqualTo(0m));
        }

        // -------------------- AplicarSobre (precio final) --------------------

        [Test]
        public void AplicarSobre_Porcentaje_RestaDescuento_YRedondea()
        {
            var d = DescuentoProducto.DesdePorcentaje(12.345m); // -> 12.35
            var final = d.AplicarSobre(100m);

            // 100 * 0.1235 = 12.35 => 100 - 12.35 = 87.65
            Assert.That(final, Is.EqualTo(87.65m));
        }

        [Test]
        public void AplicarSobre_Importe_CapAndFloor()
        {
            var d1 = DescuentoProducto.DesdeImporte(1.005m); // -> 1.01
            var final1 = d1.AplicarSobre(10m);
            Assert.That(final1, Is.EqualTo(8.99m)); // 10 - 1.01

            var d2 = DescuentoProducto.DesdeImporte(50m);
            var final2 = d2.AplicarSobre(30m);
            Assert.That(final2, Is.EqualTo(0m));     // cap => 30-30 => 0

            var final3 = d2.AplicarSobre(-10m);
            Assert.That(final3, Is.EqualTo(0m));     // base negativa => floor a 0
        }

        // -------------------- Igualdad y hash --------------------

        [Test]
        public void Igualdad_MismoTipoYMismoValor_True()
        {
            var a = DescuentoProducto.DesdePorcentaje(10m);
            var b = DescuentoProducto.DesdePorcentaje(10.000m);

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void Igualdad_EquivalentePorRedondeo_True()
        {
            var a = DescuentoProducto.DesdeImporte(1.005m); // 1.01
            var b = DescuentoProducto.DesdeImporte(1.01m);  // 1.01

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void Desigualdad_PorTipoODatosDiferentes()
        {
            var p = DescuentoProducto.DesdePorcentaje(10m);
            var i = DescuentoProducto.DesdeImporte(10m);
            var n = DescuentoProducto.Ninguno();

            Assert.That(p, Is.Not.EqualTo(i));
            Assert.That(p, Is.Not.EqualTo(n));
            Assert.That(i, Is.Not.EqualTo(n));
        }

        [Test]
        public void Equals_ContraNull_False()
        {
            var p = DescuentoProducto.DesdePorcentaje(5m);
            Assert.That(p.Equals(null), Is.False);
        }
    }
}

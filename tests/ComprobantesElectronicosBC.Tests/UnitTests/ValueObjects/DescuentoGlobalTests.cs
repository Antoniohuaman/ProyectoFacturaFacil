using System;
using NUnit.Framework;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.UnitTests.ValueObjects
{
    public class DescuentoGlobalTests
    {
        // ------------------ Fábricas ------------------

        [Test]
        public void None_EsNinguno_Y_MontoEsCero()
        {
            var d = DescuentoGlobal.None;

            Assert.Multiple(() =>
            {
                Assert.That(d.EsNinguno, Is.True);
                Assert.That(d.Modo, Is.EqualTo(DescuentoGlobalModo.Ninguno));
                Assert.That(d.Valor, Is.EqualTo(0m));
                Assert.That(d.CalcularMontoDescuento(100m), Is.EqualTo(0m));
            });
        }

        [Test]
        public void FromPorcentaje_Acepta10y100_PercFraccion()
        {
            var d10 = DescuentoGlobal.FromPorcentaje(10m);
            var d100 = DescuentoGlobal.FromPorcentaje(100m);

            Assert.Multiple(() =>
            {
                Assert.That(d10.Modo, Is.EqualTo(DescuentoGlobalModo.Porcentaje));
                Assert.That(d10.Valor, Is.EqualTo(0.10m));
                Assert.That(d100.Valor, Is.EqualTo(1.00m));
            });
        }

        [Test]
        public void FromPorcentaje_RechazaFueraDeRango()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => DescuentoGlobal.FromPorcentaje(0m));
            Assert.Throws<ArgumentOutOfRangeException>(() => DescuentoGlobal.FromPorcentaje(-5m));
            Assert.Throws<ArgumentOutOfRangeException>(() => DescuentoGlobal.FromPorcentaje(100.0001m));
        }

        [Test]
        public void FromFraccion_ValidaRangoCerradoEn1()
        {
            var d = DescuentoGlobal.FromFraccion(0.1254321m);
            Assert.Multiple(() =>
            {
                Assert.That(d.Modo, Is.EqualTo(DescuentoGlobalModo.Porcentaje));
                Assert.That(d.Valor, Is.EqualTo(0.125432m)); // redondeado a 6 decimales
            });

            var d1 = DescuentoGlobal.FromFraccion(1m);
            Assert.That(d1.Valor, Is.EqualTo(1m));
        }

        [Test]
        public void FromFraccion_RechazaFueraDeRango()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => DescuentoGlobal.FromFraccion(0m));
            Assert.Throws<ArgumentOutOfRangeException>(() => DescuentoGlobal.FromFraccion(-0.01m));
            Assert.Throws<ArgumentOutOfRangeException>(() => DescuentoGlobal.FromFraccion(1.0000001m));
        }

        [Test]
        public void FromMonto_AceptaCeroYPositivos_Redondea2()
        {
            var d0 = DescuentoGlobal.FromMonto(0m);
            var d = DescuentoGlobal.FromMonto(12.345m);

            Assert.Multiple(() =>
            {
                Assert.That(d0.Modo, Is.EqualTo(DescuentoGlobalModo.Monto));
                Assert.That(d0.Valor, Is.EqualTo(0m));
                Assert.That(d.Valor, Is.EqualTo(12.35m)); // redondeo a 2 decimales
            });
        }

        [Test]
        public void FromMonto_RechazaNegativos()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => DescuentoGlobal.FromMonto(-0.01m));
        }

        // ------------------ Cálculo ------------------

        [Test]
        public void CalcularMontoDescuento_Porcentaje_RespetaRedondeo2()
        {
            var d = DescuentoGlobal.FromPorcentaje(10m); // 10%
            var monto = d.CalcularMontoDescuento(333.33m); // 33.333 → 33.33
            var baseNeta = d.CalcularBaseLuegoDeDescuento(333.33m);

            Assert.Multiple(() =>
            {
                Assert.That(monto, Is.EqualTo(33.33m));
                Assert.That(baseNeta, Is.EqualTo(300.00m));
            });
        }

        [Test]
        public void CalcularMontoDescuento_Monto_NoExcedeSubtotal()
        {
            var d = DescuentoGlobal.FromMonto(25m);
            var monto = d.CalcularMontoDescuento(100m);

            Assert.Multiple(() =>
            {
                Assert.That(monto, Is.EqualTo(25m));
                Assert.That(d.CalcularBaseLuegoDeDescuento(100m), Is.EqualTo(75m));
            });
        }

        [Test]
        public void CalcularMontoDescuento_LanzaSiExcedeSubtotal()
        {
            var d = DescuentoGlobal.FromMonto(60m);
            Assert.Throws<InvalidOperationException>(() => d.CalcularMontoDescuento(50m));
        }

        [Test]
        public void CalcularMontoDescuento_LanzaSiSubtotalNoValido()
        {
            var d = DescuentoGlobal.FromPorcentaje(5m);
            Assert.Throws<ArgumentOutOfRangeException>(() => d.CalcularMontoDescuento(0m));
            Assert.Throws<ArgumentOutOfRangeException>(() => d.CalcularMontoDescuento(-10m));
        }

        // ------------------ Mapeo UBL (DTO) ------------------

        [Test]
        public void ToAllowanceCharge_Porcentaje_SeteaFactorYAmount()
        {
            var d = DescuentoGlobal.FromPorcentaje(12m); // 0.12
            var dto = d.ToAllowanceCharge(200m);

            Assert.Multiple(() =>
            {
                Assert.That(dto.ChargeIndicator, Is.False);
                Assert.That(dto.MultiplierFactorNumeric, Is.EqualTo(0.12m));
                Assert.That(dto.Amount, Is.EqualTo(24.00m)); // 200 * 0.12
            });
        }

        [Test]
        public void ToAllowanceCharge_Monto_NoIncluyeFactor()
        {
            var d = DescuentoGlobal.FromMonto(15.5m);
            var dto = d.ToAllowanceCharge(200m);

            Assert.Multiple(() =>
            {
                Assert.That(dto.ChargeIndicator, Is.False);
                Assert.That(dto.MultiplierFactorNumeric.HasValue, Is.False);
                Assert.That(dto.Amount, Is.EqualTo(15.50m));
            });
        }

        [Test]
        public void ToAllowanceCharge_None_AmountCero()
        {
            var dto = DescuentoGlobal.None.ToAllowanceCharge(100m);
            Assert.Multiple(() =>
            {
                Assert.That(dto.ChargeIndicator, Is.False);
                Assert.That(dto.Amount, Is.EqualTo(0m));
                Assert.That(dto.MultiplierFactorNumeric.HasValue, Is.False);
            });
        }

        // ------------------ Equality / ToString ------------------

        [Test]
        public void Equality_MismoContenido_Igual()
        {
            var a = DescuentoGlobal.FromPorcentaje(10m);
            var b = DescuentoGlobal.FromFraccion(0.10m);
            Assert.That(a, Is.EqualTo(b));

            var c = DescuentoGlobal.FromMonto(5m);
            var d = DescuentoGlobal.FromMonto(5.00m);
            Assert.That(c, Is.EqualTo(d));
        }

        [Test]
        public void ToString_FormateaCorrecto()
        {
            var none = DescuentoGlobal.None.ToString();
            var p10 = DescuentoGlobal.FromPorcentaje(10m).ToString();
            var m5 = DescuentoGlobal.FromMonto(5m).ToString();

            Assert.Multiple(() =>
            {
                Assert.That(none, Is.EqualTo("Sin descuento"));
                Assert.That(p10, Is.EqualTo("10%"));
                Assert.That(m5, Is.EqualTo("Monto: 5.00"));
            });
        }
    }
}

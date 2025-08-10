using System;
using NUnit.Framework;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.UnitTests.ValueObjects
{
    [TestFixture]
    public class MonedaTests
    {
        [Test]
        public void Factories_PEN_y_USD_devuelven_objetos_correctos()
        {
            var pen = Moneda.PEN();
            var usd = Moneda.USD();

            Assert.Multiple(() =>
            {
                Assert.That(pen.Codigo, Is.EqualTo("PEN"));
                Assert.That(pen.Simbolo, Is.EqualTo("S/."));
                Assert.That(pen.Decimales, Is.EqualTo((byte)2));
                Assert.That(pen.Nombre, Is.EqualTo("Soles"));
                Assert.That(pen.EsPEN, Is.True);
                Assert.That(pen.EsUSD, Is.False);

                Assert.That(usd.Codigo, Is.EqualTo("USD"));
                Assert.That(usd.Simbolo, Is.EqualTo("US$"));
                Assert.That(usd.Decimales, Is.EqualTo((byte)2));
                Assert.That(usd.Nombre, Is.EqualTo("Dólares"));
                Assert.That(usd.EsUSD, Is.True);
                Assert.That(usd.EsPEN, Is.False);
            });
        }

        [Test]
        public void Create_Normaliza_ISO_y_valida_soporte_PEN_USD()
        {
            var m1 = Moneda.Create(" pen "); // normaliza a "PEN"
            var m2 = Moneda.Create("USD");

            Assert.Multiple(() =>
            {
                Assert.That(m1.Codigo, Is.EqualTo("PEN"));
                Assert.That(m2.Codigo, Is.EqualTo("USD"));
            });
        }

        [Test]
        public void Create_Lanza_para_monedas_no_soportadas()
        {
            // No soportadas por ahora
            Assert.Throws<ArgumentException>(() => Moneda.Create("eur"));
            Assert.Throws<ArgumentException>(() => Moneda.Create("PENN"));
            Assert.Throws<ArgumentException>(() => Moneda.Create("P3N"));
            Assert.Throws<ArgumentException>(() => Moneda.Create("  "));
        }

        [Test]
        public void CreateCustom_Permite_extender_con_invariantes_basicas()
        {
            var mxn = Moneda.CreateCustom("MXN", "$", 2, "Pesos Mexicanos");

            Assert.Multiple(() =>
            {
                Assert.That(mxn.Codigo, Is.EqualTo("MXN"));
                Assert.That(mxn.Simbolo, Is.EqualTo("$"));
                Assert.That(mxn.Decimales, Is.EqualTo((byte)2));
                Assert.That(mxn.Nombre, Is.EqualTo("Pesos Mexicanos"));
            });
        }

        [Test]
        public void CreateCustom_Valida_simbolo_y_rango_de_decimales()
        {
            Assert.Throws<ArgumentException>(() => Moneda.CreateCustom("CLP", "", 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Moneda.CreateCustom("CLP", "$", 5));
            Assert.Throws<ArgumentException>(() => Moneda.CreateCustom("CLPX", "$", 2));  // ISO inválido
            Assert.Throws<ArgumentException>(() => Moneda.CreateCustom("C1P", "$", 2));   // ISO inválido
        }

        [Test]
        public void ToString_Muestra_nombre_si_existe()
        {
            var pen = Moneda.PEN();
            var usd = Moneda.USD();
            var clp = Moneda.CreateCustom("CLP", "$", 0); // sin nombre

            Assert.Multiple(() =>
            {
                Assert.That(pen.ToString(), Is.EqualTo("PEN (Soles)"));
                Assert.That(usd.ToString(), Is.EqualTo("USD (Dólares)"));
                Assert.That(clp.ToString(), Is.EqualTo("CLP"));
            });
        }

        [Test]
        public void Redondear_Usa_AwayFromZero_con_decimales_de_la_moneda()
        {
            var pen = Moneda.PEN(); // 2 decimales

            // AwayFromZero: 1.005 -> 1.01 ; -1.005 -> -1.01
            Assert.Multiple(() =>
            {
                Assert.That(pen.Redondear(1.005m), Is.EqualTo(1.01m));
                Assert.That(pen.Redondear(1.004m), Is.EqualTo(1.00m));
                Assert.That(pen.Redondear(-1.005m), Is.EqualTo(-1.01m));
            });

            // Moneda con 0 decimales
            var clp = Moneda.CreateCustom("CLP", "$", 0);
            Assert.Multiple(() =>
            {
                Assert.That(clp.Redondear(1234.6m), Is.EqualTo(1235m));
                Assert.That(clp.Redondear(1234.4m), Is.EqualTo(1234m));
            });
        }

        [Test]
        public void Formatear_Usa_InvariantCulture_y_respeta_separadores()
        {
            var pen = Moneda.PEN();
            var usd = Moneda.USD();

            var monto = 1234.5m;

            // Con separadores (N2, InvariantCulture) → "1,234.50"
            var sPen = pen.Formatear(monto, incluirSeparadores: true);
            var sUsd = usd.Formatear(monto, incluirSeparadores: true);

            // Sin separadores (F2) → "1234.50"
            var sUsdNoSep = usd.Formatear(monto, incluirSeparadores: false);

            Assert.Multiple(() =>
            {
                Assert.That(sPen, Is.EqualTo("S/. 1,234.50"));
                Assert.That(sUsd, Is.EqualTo("US$ 1,234.50"));
                Assert.That(sUsdNoSep, Is.EqualTo("US$ 1234.50"));
            });
        }
    }
}

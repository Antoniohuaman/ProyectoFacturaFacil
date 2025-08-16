using System;
using System.Globalization;
using NUnit.Framework;
using SharedKernel.ValueObjects;

// 👇 añade estos alias para usar el SharedKernel
using MonedaSK = SharedKernel.ValueObjects.Moneda;
using DineroSK = SharedKernel.ValueObjects.Dinero;


namespace SharedKernel.Tests.ValueObjects
{
    [TestFixture]
    public class DineroTests
    {
        private Moneda PEN => Moneda.PEN();
        private Moneda USD => Moneda.USD();

        [SetUp]
        public void SetUp()
        {
            // Evita fallos por separador decimal en distintas culturas
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        }

        // --- Creación y normalización (redondeo AwayFromZero) ---

        [Test]
        public void Create_Redondea_A_DosDecimales_Positivo()
        {
            var d = Dinero.Create(1.005m, PEN); // 2 decimales
            Assert.That(d.Monto, Is.EqualTo(1.01m));
            Assert.That(d.Moneda, Is.EqualTo(PEN));
        }

        [Test]
        public void Create_Redondea_A_DosDecimales_Negativo()
        {
            var d = Dinero.Create(-1.005m, PEN);
            Assert.That(d.Monto, Is.EqualTo(-1.01m));
            Assert.That(d.Moneda, Is.EqualTo(PEN));
        }

        [Test]
        public void Create_Respeta_Decimales_DeLaMoneda()
        {
            var CLP = Moneda.Create("CLP", "$", decimales: 0); // moneda sin decimales
            var d = Dinero.Create(123.7m, CLP);
            Assert.That(d.Monto, Is.EqualTo(124m)); // 0 decimales → 124
            Assert.That(d.Moneda, Is.EqualTo(CLP));
        }

        [Test]
        public void Cero_RegresaMontoCero_EnMoneda()
        {
            var d = Dinero.Cero(PEN);
            Assert.That(d.Monto, Is.EqualTo(0m));
            Assert.That(d.Moneda, Is.EqualTo(PEN));
            Assert.That(d.EsCero, Is.True);
        }

        // --- Aritmética misma moneda ---

        [Test]
        public void Sumar_MismaMoneda_Ok()
        {
            var a = Dinero.Create(10.10m, PEN);
            var b = Dinero.Create(5.25m, PEN);
            var r = a.Sumar(b);
            Assert.That(r.Monto, Is.EqualTo(15.35m));
            Assert.That(r.Moneda, Is.EqualTo(PEN));
        }

        [Test]
        public void Restar_MismaMoneda_Ok()
        {
            var a = Dinero.Create(10.10m, PEN);
            var b = Dinero.Create(5.25m, PEN);
            var r = a.Restar(b);
            Assert.That(r.Monto, Is.EqualTo(4.85m));
            Assert.That(r.Moneda, Is.EqualTo(PEN));
        }

        [Test]
        public void Negar_CambiaSigno_PreservaMoneda()
        {
            var a = Dinero.Create(10.00m, PEN);
            var r = a.Negar();
            Assert.That(r.Monto, Is.EqualTo(-10.00m));
            Assert.That(r.Moneda, Is.EqualTo(PEN));
        }

        [Test]
        public void Multiplicar_PorEscalar_RedondeaAlFinal()
        {
            var a = Dinero.Create(10.00m, PEN);
            var r = a.Multiplicar(1.333m); // 13.33
            Assert.That(r.Monto, Is.EqualTo(13.33m));
            Assert.That(r.Moneda, Is.EqualTo(PEN));
        }

        [Test]
        public void Dividir_PorEscalar_RedondeaAlFinal()
        {
            var a = Dinero.Create(10.00m, PEN);
            var r = a.Dividir(3m); // 3.33
            Assert.That(r.Monto, Is.EqualTo(3.33m));
            Assert.That(r.Moneda, Is.EqualTo(PEN));
        }

        [Test]
        public void Dividir_EntreCero_Lanza()
        {
            var a = Dinero.Create(10.00m, PEN);
            Assert.Throws<DivideByZeroException>(() => a.Dividir(0m));
        }

        // --- Operadores ---

        [Test]
        public void Operadores_Suma_Resta_Multiplicar_Dividir_Funcionan()
        {
            var a = Dinero.Create(10.00m, PEN);
            var b = Dinero.Create(2.55m, PEN);

            var sum = a + b;   // 12.55
            var res = a - b;   // 7.45
            var mul = a * 2.5m; // 25.00
            var mul2 = 2.5m * a; // 25.00
            var div = a / 4m;  // 2.50
            var neg = -a;      // -10.00

            Assert.Multiple(() =>
            {
                Assert.That(sum.Monto, Is.EqualTo(12.55m));
                Assert.That(res.Monto, Is.EqualTo(7.45m));
                Assert.That(mul.Monto, Is.EqualTo(25.00m));
                Assert.That(mul2.Monto, Is.EqualTo(25.00m));
                Assert.That(div.Monto, Is.EqualTo(2.50m));
                Assert.That(neg.Monto, Is.EqualTo(-10.00m));
            });
        }

        [Test]
        public void Abs_DevuelveValorAbsoluto()
        {
            var a = Dinero.Create(-7.49m, PEN);
            var r = a.Abs();
            Assert.That(r.Monto, Is.EqualTo(7.49m));
            Assert.That(r.Moneda, Is.EqualTo(PEN));
        }

        // --- Monedas distintas lanzan ---

        [Test]
        public void Sumar_MonedasDistintas_Lanza()
        {
            var a = Dinero.Create(10m, PEN);
            var b = Dinero.Create(5m, USD);
            Assert.Throws<InvalidOperationException>(() => _ = a.Sumar(b));
        }

        [Test]
        public void Restar_MonedasDistintas_Lanza()
        {
            var a = Dinero.Create(10m, PEN);
            var b = Dinero.Create(5m, USD);
            Assert.Throws<InvalidOperationException>(() => _ = a.Restar(b));
        }

        // --- Monedas iguales por valor (distintas instancias) deben permitir operar ---

        [Test]
        public void Sumar_MonedasIgualesPorValor_Permitido()
        {
            var pen1 = Moneda.PEN();
            var pen2 = Moneda.Create("PEN"); // instancia distinta pero igual por valor

            var a = Dinero.Create(1.10m, pen1);
            var b = Dinero.Create(2.20m, pen2);

            var r = a + b;
            Assert.That(r.Monto, Is.EqualTo(3.30m));
            Assert.That(r.Moneda, Is.EqualTo(pen1)); // la del operando izquierdo
        }

        // --- Dividir en partes (prorrateo en unidades mínimas) ---

        [Test]
        public void DividirEnPartes_Reparte_Centavos_Exactamente_SumaIgualOriginal()
        {
            var total = Dinero.Create(1.00m, PEN);
            var partes = total.DividirEnPartes(3); // 0.34, 0.33, 0.33

            Assert.That(partes.Count, Is.EqualTo(3));
            Assert.That(partes[0].Monto, Is.EqualTo(0.34m));
            Assert.That(partes[1].Monto, Is.EqualTo(0.33m));
            Assert.That(partes[2].Monto, Is.EqualTo(0.33m));

            var suma = partes[0] + partes[1] + partes[2];
            Assert.That(suma.Monto, Is.EqualTo(total.Monto));
            Assert.That(suma.Moneda, Is.EqualTo(PEN));
        }

        [Test]
        public void DividirEnPartes_MontoNegativo_Reparte_UnidadesMinimas_NegativasPrimero()
        {
            var total = Dinero.Create(-1.00m, PEN);
            var partes = total.DividirEnPartes(3); // -0.34, -0.33, -0.33

            Assert.That(partes[0].Monto, Is.EqualTo(-0.34m));
            Assert.That(partes[1].Monto, Is.EqualTo(-0.33m));
            Assert.That(partes[2].Monto, Is.EqualTo(-0.33m));

            var suma = partes[0] + partes[1] + partes[2];
            Assert.That(suma.Monto, Is.EqualTo(total.Monto));
        }

        [Test]
        public void DividirEnPartes_MasPartesQueUnidadesMinimas_ReparteBien()
        {
            var total = Dinero.Create(0.01m, PEN);
            var partes = total.DividirEnPartes(3); // 0.01, 0.00, 0.00

            Assert.That(partes[0].Monto, Is.EqualTo(0.01m));
            Assert.That(partes[1].Monto, Is.EqualTo(0.00m));
            Assert.That(partes[2].Monto, Is.EqualTo(0.00m));

            var suma = partes[0] + partes[1] + partes[2];
            Assert.That(suma.Monto, Is.EqualTo(0.01m));
        }

        [Test]
        public void DividirEnPartes_PartesInvalidas_Lanza()
        {
            var total = Dinero.Create(10m, PEN);
            Assert.Throws<ArgumentOutOfRangeException>(() => total.DividirEnPartes(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => total.DividirEnPartes(-2));
        }

        // --- ToString ---

        [Test]
        public void ToString_UsaSimboloYDecimalesDeLaMoneda()
        {
            var d1 = Dinero.Create(1234.5m, PEN); // "S/. 1234.50"
            var d2 = Dinero.Create(99m, Moneda.Create("CLP", "$", 0)); // "$ 99"

            Assert.That(d1.ToString(), Is.EqualTo("S/. 1234.50"));
            Assert.That(d2.ToString(), Is.EqualTo("$ 99"));
        }
    }
}

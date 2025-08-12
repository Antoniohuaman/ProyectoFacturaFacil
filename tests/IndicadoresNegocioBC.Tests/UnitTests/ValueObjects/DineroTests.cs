using System;
using System.Linq;
using IndicadoresNegocioBC.Domain.ValueObjects;
using NUnit.Framework;

namespace IndicadoresNegocioBC.Tests.UnitTests.ValueObjects
{
    public class DineroTests
    {
        private static Moneda PEN => new Moneda("PEN");
        private static Moneda USD => new Moneda("USD");

        [Test]
        public void Crear_MonedaNula_LanzaArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Dinero.Crear(10m, null!));
        }

        [Test]
        public void Crear_NormalizaEscala_DosDecimales_MidpointToEven()
        {
            // 10.005 -> 10.01 (con AwayFromZero)
            var d1 = Dinero.Crear(10.005m, PEN);
            Assert.That(d1.Monto, Is.EqualTo(10.01m));

            // 10.015 -> 10.02
            var d2 = Dinero.Crear(10.015m, PEN);
            Assert.That(d2.Monto, Is.EqualTo(10.02m));
        }

        [Test]
        public void Cero_CreaConMontoCero()
        {
            var d = Dinero.Cero(PEN);
            Assert.That(d.Monto, Is.EqualTo(0m));
            Assert.That(d.Moneda, Is.EqualTo(PEN));
            Assert.That(d.EsCero, Is.True);
        }

        [Test]
        public void Sumar_MismaMoneda_SumaCorrecta()
        {
            var a = Dinero.Crear(10.10m, PEN);
            var b = Dinero.Crear(5.25m, PEN);

            var r = a.Sumar(b);
            Assert.That(r.Moneda, Is.EqualTo(PEN));
            Assert.That(r.Monto, Is.EqualTo(15.35m));
        }

        [Test]
        public void Restar_MismaMoneda_RestaCorrecta()
        {
            var a = Dinero.Crear(20.00m, PEN);
            var b = Dinero.Crear(5.99m, PEN);

            var r = a.Restar(b);
            Assert.That(r.Moneda, Is.EqualTo(PEN));
            Assert.That(r.Monto, Is.EqualTo(14.01m));
        }

        [Test]
        public void Operar_MonedasDistintas_LanzaInvalidOperationException()
        {
            var a = Dinero.Crear(10m, PEN);
            var b = Dinero.Crear(2m, USD);

            Assert.Throws<InvalidOperationException>(() => { var _ = a.Sumar(b); });
            Assert.Throws<InvalidOperationException>(() => { var _ = a.Restar(b); });
            Assert.Throws<InvalidOperationException>(() => { var _ = a + b; });
            Assert.Throws<InvalidOperationException>(() => { var _ = a - b; });
        }

        [Test]
        public void Multiplicar_AplicaRedondeoADosDecimales()
        {
            var a = Dinero.Crear(100.333m, PEN);
            var r = a.Multiplicar(3m); // 100.33*3=300.999 -> 300.99 con AwayFromZero
            Assert.That(r.Monto, Is.EqualTo(300.99m));
            Assert.That(r.Moneda, Is.EqualTo(PEN));
        }

        [Test]
        public void Dividir_AplicaRedondeoADosDecimales()
        {
            var a = Dinero.Crear(10m, PEN);
            var r = a.Dividir(3m); // 3.3333 -> 3.33
            Assert.That(r.Monto, Is.EqualTo(3.33m));
            Assert.That(r.Moneda, Is.EqualTo(PEN));
        }

        [Test]
        public void Dividir_PorCero_LanzaDivideByZeroException()
        {
            var a = Dinero.Crear(10m, PEN);
            Assert.Throws<DivideByZeroException>(() => a.Dividir(0m));
            Assert.Throws<DivideByZeroException>(() => { var _ = a / 0m; });
        }

        [Test]
        public void Negativo_CambiaSigno_PreservaMoneda()
        {
            var a = Dinero.Crear(12.34m, PEN);
            var n = a.Negativo();

            Assert.That(n.Monto, Is.EqualTo(-12.34m));
            Assert.That(n.Moneda, Is.EqualTo(PEN));
        }

        [Test]
        public void Abs_DevuelvePositivo()
        {
            var neg = Dinero.Crear(-9.99m, PEN);
            var abs = neg.Abs();
            Assert.That(abs.Monto, Is.EqualTo(9.99m));
            Assert.That(abs.Moneda, Is.EqualTo(PEN));
        }

        [Test]
        public void Predicados_EsCero_EsPositivo_EsNegativo_Correctos()
        {
            var cero = Dinero.Crear(0m, PEN);
            var pos  = Dinero.Crear(0.01m, PEN);
            var neg  = Dinero.Crear(-0.01m, PEN);

            Assert.Multiple(() =>
            {
                Assert.That(cero.EsCero, Is.True);
                Assert.That(cero.EsPositivo, Is.False);
                Assert.That(cero.EsNegativo, Is.False);

                Assert.That(pos.EsCero, Is.False);
                Assert.That(pos.EsPositivo, Is.True);
                Assert.That(pos.EsNegativo, Is.False);

                Assert.That(neg.EsCero, Is.False);
                Assert.That(neg.EsPositivo, Is.False);
                Assert.That(neg.EsNegativo, Is.True);
            });
        }

        [Test]
        public void Igualdad_PorValor_MismoMontoYMoneda()
        {
            var a = Dinero.Crear(10.00m, PEN);
            var b = Dinero.Crear(10.0m, PEN); // misma cantidad
            Assert.That(a, Is.EqualTo(b));    // record: igualdad por valor
        }

        [Test]
        public void Operadores_SumarRestarMultiplicarDividir_EquivalentesAMetodos()
        {
            var a = Dinero.Crear(10m, PEN);
            var b = Dinero.Crear(2.50m, PEN);

            Assert.That(a + b, Is.EqualTo(a.Sumar(b)));
            Assert.That(a - b, Is.EqualTo(a.Restar(b)));
            Assert.That(a * 3m, Is.EqualTo(a.Multiplicar(3m)));
            Assert.That(3m * a, Is.EqualTo(a.Multiplicar(3m)));
            Assert.That(a / 4m, Is.EqualTo(a.Dividir(4m)));
        }

        [Test]
        public void Prorratear_Positivo_DistribuyeCentavosYConservaSuma()
        {
            var total = Dinero.Crear(10.00m, PEN);
            var partes = total.Prorratear(3);

            Assert.That(partes.Count, Is.EqualTo(3));

            // Esperado típico: 3.34, 3.33, 3.33 (los primeros reciben el centavo sobrante)
            Assert.Multiple(() =>
            {
                Assert.That(partes[0].Monto, Is.EqualTo(3.34m));
                Assert.That(partes[1].Monto, Is.EqualTo(3.33m));
                Assert.That(partes[2].Monto, Is.EqualTo(3.33m));
                Assert.That(partes.All(p => p.Moneda == PEN), Is.True);
                Assert.That(partes.Aggregate(Dinero.Cero(PEN), (acc, x) => acc + x), Is.EqualTo(total));
            });
        }

        [Test]
        public void Prorratear_Negativo_DistribuyeCentavosYConservaSuma()
        {
            var total = Dinero.Crear(-10.00m, PEN);
            var partes = total.Prorratear(3);

            Assert.That(partes.Count, Is.EqualTo(3));

            // Esperado: -3.34, -3.33, -3.33 y suma = -10.00
            Assert.Multiple(() =>
            {
                Assert.That(partes[0].Monto, Is.EqualTo(-3.34m));
                Assert.That(partes[1].Monto, Is.EqualTo(-3.33m));
                Assert.That(partes[2].Monto, Is.EqualTo(-3.33m));
                Assert.That(partes.All(p => p.Moneda == PEN), Is.True);
                Assert.That(partes.Aggregate(Dinero.Cero(PEN), (acc, x) => acc + x), Is.EqualTo(total));
            });
        }

        [Test]
        public void Prorratear_PartesInvalidas_Lanza()
        {
            var total = Dinero.Crear(100m, PEN);
            Assert.Throws<ArgumentOutOfRangeException>(() => total.Prorratear(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => total.Prorratear(-5));
        }

        [Test]
        public void ToString_FormatoInvariante()
        {
            var d = Dinero.Crear(1234.5m, PEN);
            Assert.That(d.ToString(), Is.EqualTo("PEN 1234.50"));
        }
    }
}



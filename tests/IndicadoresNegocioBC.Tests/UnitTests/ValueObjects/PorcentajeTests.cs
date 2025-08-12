using System;
using IndicadoresNegocioBC.Domain.ValueObjects;
using NUnit.Framework;

namespace IndicadoresNegocioBC.Tests.UnitTests.ValueObjects
{
    public class PorcentajeTests
    {
        private static Moneda PEN => new Moneda("PEN");

        // ---------- Construcción / escala / redondeo ----------

        [Test]
        public void DesdeFraccion_NormalizaAEscalaSeis_MidpointToEven()
        {
            // 0.1234565 -> a 6 decimales con ToEven:
            // dígito previo = 6 (par) y no hay más dígitos => se mantiene
            var p1 = Porcentaje.DesdeFraccion(0.1234565m);
            Assert.That(p1.Fraccion, Is.EqualTo(0.123456m));

            // 0.12345655 -> hay más dígitos después del 5 => redondea hacia arriba
            var p2 = Porcentaje.DesdeFraccion(0.12345655m);
            Assert.That(p2.Fraccion, Is.EqualTo(0.123457m));
        }

        [Test]
        public void DesdePorCiento_ConvierteCorrectamente()
        {
            var p = Porcentaje.DesdePorCiento(12.34m);
            Assert.That(p.Fraccion, Is.EqualTo(0.1234m));
        }

        [Test]
        public void InstanciasComunes_Cero_Cincuenta_Cien()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Porcentaje.Cero.Fraccion, Is.EqualTo(0m));
                Assert.That(Porcentaje.Cincuenta.Fraccion, Is.EqualTo(0.5m));
                Assert.That(Porcentaje.Cien.Fraccion, Is.EqualTo(1m));
            });
        }

        // ---------- Operaciones con Dinero ----------

        [Test]
        public void Aplicar_A_Dinero_CalculaImporteRedondeadoADosDecimales()
        {
            var p = Porcentaje.DesdePorCiento(12.5m);   // 0.125
            var importe = Dinero.Crear(100m, PEN);      // PEN 100.00

            var r = p.Aplicar(importe);                 // 12.5 -> PEN 12.50

            Assert.That(r.Moneda, Is.EqualTo(PEN));
            Assert.That(r.Monto, Is.EqualTo(12.50m));
        }

        // ---------- Operaciones entre porcentajes ----------

        [Test]
        public void Sumar_Restar_Multiplicar_Negativo_Correctos()
        {
            var a = Porcentaje.DesdePorCiento(10m);   // 0.10
            var b = Porcentaje.DesdePorCiento(5m);    // 0.05

            var suma = a.Sumar(b);        // 0.15
            var resta = a.Restar(b);      // 0.05
            var escalar = a.Multiplicar(2m); // 0.20
            var neg = a.Negativo();       // -0.10

            Assert.Multiple(() =>
            {
                Assert.That(suma.Fraccion, Is.EqualTo(0.15m));
                Assert.That(resta.Fraccion, Is.EqualTo(0.05m));
                Assert.That(escalar.Fraccion, Is.EqualTo(0.20m));
                Assert.That(neg.Fraccion, Is.EqualTo(-0.10m));
            });
        }

        [Test]
        public void Operadores_EquivalenAMetodos()
        {
            var a = Porcentaje.DesdePorCiento(10m); // 0.10
            var b = Porcentaje.DesdePorCiento(2m);  // 0.02

            Assert.Multiple(() =>
            {
                Assert.That(a + b, Is.EqualTo(a.Sumar(b)));
                Assert.That(a - b, Is.EqualTo(a.Restar(b)));
                Assert.That(-a, Is.EqualTo(a.Negativo()));
                Assert.That(a * 3m, Is.EqualTo(a.Multiplicar(3m)));
                Assert.That(3m * a, Is.EqualTo(a.Multiplicar(3m)));
            });
        }

        // ---------- Límites ----------

        [Test]
        public void Limitar_AcotaAlRango()
        {
            var p = Porcentaje.DesdePorCiento(150m); // 1.5

            var limitado1 = p.Limitar(0m, 1m);       // tope 100%
            var limitado2 = Porcentaje.DesdePorCiento(-25m).Limitar(0m, 1m); // sube a 0%

            Assert.Multiple(() =>
            {
                Assert.That(limitado1.Fraccion, Is.EqualTo(1m));
                Assert.That(limitado2.Fraccion, Is.EqualTo(0m));
            });
        }

        [Test]
        public void Limitar_MinMayorQueMax_Lanza()
        {
            var p = Porcentaje.DesdePorCiento(10m);
            Assert.Throws<ArgumentException>(() => p.Limitar(1m, 0m));
        }

        // ---------- Conversión / formato ----------

        [Test]
        public void APorCiento_Y_Formatear_Y_ToString()
        {
            var p = Porcentaje.DesdeFraccion(0.123456m); // 12.3456%

            Assert.Multiple(() =>
            {
                Assert.That(p.APorCiento(), Is.EqualTo(12.35m));            // redondeo a 2
                Assert.That(p.Formatear(), Is.EqualTo("12.35 %"));
                Assert.That(p.ToString(), Is.EqualTo("12.35 %"));
                Assert.That(p.Formatear(1), Is.EqualTo("12.3 %"));
                Assert.That(p.Formatear(3), Is.EqualTo("12.346 %"));
            });
        }

        // ---------- Igualdad ----------

        [Test]
        public void Igualdad_PorValor_MismaFraccionTrasNormalizacion()
        {
            var a = Porcentaje.DesdeFraccion(0.1000004m); // -> 0.100000
            var b = Porcentaje.DesdeFraccion(0.1m);       // -> 0.100000
            Assert.That(a, Is.EqualTo(b));
        }
    }
}
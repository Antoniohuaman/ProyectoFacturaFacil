using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using IndicadoresNegocioBC.Domain.ValueObjects;

namespace IndicadoresNegocioBC.Tests.Domain.ValueObjects
{
    [TestFixture]
    public class PorcentajeTests
    {
        [Test]
        public void DesdeFraccion_Deberia_Normalizar_A_Escala6_Y_ToEven()
        {
            // Redondeo hacia arriba (7 en la 7ma cifra)
            var p1 = Porcentaje.DesdeFraccion(0.123456789m);
            Assert.That(p1.Fraccion, Is.EqualTo(0.123457m));

            // Empate 5 exacto con dígito anterior PAR -> NO sube (ToEven)
            var p2 = Porcentaje.DesdeFraccion(0.1234565m); // 6ta cifra: 6 (par), siguiente: 5 exacto
            Assert.That(p2.Fraccion, Is.EqualTo(0.123456m));

            // Empate 5 exacto con dígito anterior IMPAR + cola -> SÍ sube
            var p3 = Porcentaje.DesdeFraccion(0.1234555m); // 6ta cifra: 5 (impar), siguiente no-cero
            Assert.That(p3.Fraccion, Is.EqualTo(0.123456m));

            // Negativos también se normalizan
            var p4 = Porcentaje.DesdeFraccion(-0.87654321m);
            Assert.That(p4.Fraccion, Is.EqualTo(-0.876543m));
        }

        [Test]
        public void DesdePorCiento_Deberia_Dividir_Entre_100_Y_Respetar_Escala()
        {
            var p = Porcentaje.DesdePorCiento(12.345678m);
            Assert.That(p.Fraccion, Is.EqualTo(0.123457m)); // 12.345678% -> 0.12345678 -> 0.123457
        }

        [Test]
        public void Instancias_Comunes_Deberian_Tener_Fraccion_Esperada()
        {
            Assert.That(Porcentaje.Cero.Fraccion, Is.EqualTo(0m));
            Assert.That(Porcentaje.Cincuenta.Fraccion, Is.EqualTo(0.5m));
            Assert.That(Porcentaje.Cien.Fraccion, Is.EqualTo(1m));
        }

        [Test]
        public void Operaciones_Sumar_Restar_Multiplicar_Y_Negativo_Deberian_Respetar_Escala()
        {
            var a = Porcentaje.DesdeFraccion(0.123456m);
            var b = Porcentaje.DesdeFraccion(0.100001m);

            var sum = a.Sumar(b);
            Assert.That(sum.Fraccion, Is.EqualTo(0.223457m));

            var res = a.Restar(b);
            Assert.That(res.Fraccion, Is.EqualTo(0.023455m));

            var mul = a.Multiplicar(2m);
            Assert.That(mul.Fraccion, Is.EqualTo(0.246912m)); // exacto a 6 decimales

            var neg = a.Negativo();
            Assert.That(neg.Fraccion, Is.EqualTo(-0.123456m));
        }

        [Test]
        public void Limitar_Deberia_Clampear_A_Rango_Y_Validar_MinMax()
        {
            var p = Porcentaje.DesdeFraccion(1.5m); // 150%
            var clamped = p.Limitar(0m, 1m);
            Assert.That(clamped.Fraccion, Is.EqualTo(1m));

            var p2 = Porcentaje.DesdeFraccion(-0.30m);
            var clamped2 = p2.Limitar(-0.25m, 0.25m);
            Assert.That(clamped2.Fraccion, Is.EqualTo(-0.25m));

            Assert.That(() => p.Limitar(0.8m, 0.2m), Throws.ArgumentException);
        }

        [Test]
        public void APorCiento_Y_Formatear_Y_ToString_Deberian_Formatear_Correcto_Invariant()
        {
            var p = Porcentaje.DesdeFraccion(0.123456m); // 12.3456%
            Assert.That(p.APorCiento(2), Is.EqualTo(12.35m)); // ToEven a 2 decimales
            Assert.That(p.Formatear(2), Is.EqualTo("12.35 %"));

            var p2 = Porcentaje.DesdeFraccion(0.5m); // 50%
            Assert.That(p2.ToString(), Is.EqualTo("50.00 %"));

            var old = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("es-PE"); // Usamos Invariant en Formatear
                Assert.That(p2.Formatear(3), Is.EqualTo("50.000 %"));
            }
            finally
            {
                CultureInfo.CurrentCulture = old;
            }
        }

        [Test]
        public void Operadores_Deberian_Delegar_A_Las_Mismas_Operaciones()
        {
            var a = Porcentaje.DesdeFraccion(0.10m);
            var b = Porcentaje.DesdeFraccion(0.05m);

            Assert.That((a + b).Fraccion, Is.EqualTo(0.15m));
            Assert.That((a - b).Fraccion, Is.EqualTo(0.05m));
            Assert.That((-a).Fraccion, Is.EqualTo(-0.10m));
            Assert.That((a * 2m).Fraccion, Is.EqualTo(0.20m));
            Assert.That((2m * b).Fraccion, Is.EqualTo(0.10m));
        }

        [Test]
        public void Igualdad_Por_Valor_Deberia_Cumplirse()
        {
            var a = Porcentaje.DesdeFraccion(0.12m);
            var b = Porcentaje.DesdePorCiento(12m);        // 0.12
            var c = Porcentaje.DesdeFraccion(0.1200004m);  // redondea a 0.120000 -> 0.12

            Assert.That(a.Fraccion, Is.EqualTo(0.12m));
            Assert.That(b.Fraccion, Is.EqualTo(0.12m));
            Assert.That(c.Fraccion, Is.EqualTo(0.12m));

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a, Is.EqualTo(c));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        // =========================
        // Integración con Dinero (opcional, via reflexión)
        // =========================

    }
}

using System;
using IndicadoresNegocioBC.Domain.ValueObjects;
using NUnit.Framework;

namespace IndicadoresNegocioBC.Tests.UnitTests.ValueObjects
{
    public class TicketPromedioTests
    {
        private static Moneda PEN => new Moneda("PEN");
        private static Moneda USD => new Moneda("USD");

        // -------- Fábricas / Invariantes --------

        [Test]
        public void Crear_Valido_EstableceMontoTotalYCantidad()
        {
            var total = Dinero.Crear(100m, PEN);
            var tp = TicketPromedio.Crear(total, 4);

            Assert.Multiple(() =>
            {
                Assert.That(tp.MontoTotal, Is.EqualTo(total));
                Assert.That(tp.Moneda, Is.EqualTo(PEN));
                Assert.That(tp.CantidadComprobantes, Is.EqualTo(4));
                Assert.That(tp.TieneDatos, Is.True);
            });
        }

        [Test]
        public void Crear_CantidadNegativa_Lanza()
        {
            var total = Dinero.Crear(0m, PEN);
            Assert.Throws<ArgumentOutOfRangeException>(() => TicketPromedio.Crear(total, -1));
        }

        [Test]
        public void Crear_CantidadCero_TotalNoCero_Lanza()
        {
            var total = Dinero.Crear(10m, PEN);
            Assert.Throws<ArgumentException>(() => TicketPromedio.Crear(total, 0));
        }

        [Test]
        public void Vacio_MonedaRequerida_TotalYCantidadEnCero()
        {
            var tp = TicketPromedio.Vacio(PEN);

            Assert.Multiple(() =>
            {
                Assert.That(tp.Moneda, Is.EqualTo(PEN));
                Assert.That(tp.MontoTotal.Monto, Is.EqualTo(0m));
                Assert.That(tp.CantidadComprobantes, Is.EqualTo(0));
                Assert.That(tp.TieneDatos, Is.False);
                Assert.That(tp.Promedio.Monto, Is.EqualTo(0m));
            });
        }

        // -------- Promedio --------

        [Test]
        public void Promedio_CuandoHayDatos_EsTotalEntreCantidad_Redondeado()
        {
            var total = Dinero.Crear(10m, PEN);
            var tp = TicketPromedio.Crear(total, 3);

            // 10 / 3 = 3.333.. -> 3.33 con redondeo bancario a 2
            Assert.That(tp.Promedio, Is.EqualTo(Dinero.Crear(3.33m, PEN)));
        }

        [Test]
        public void Promedio_SinDatos_EsCero()
        {
            var tp = TicketPromedio.Vacio(PEN);
            Assert.That(tp.Promedio.Monto, Is.EqualTo(0m));
        }

        // -------- Agregar / Quitar venta --------

        [Test]
        public void AgregarVenta_SumaImporteEIncrementaContador()
        {
            var tp = TicketPromedio.Vacio(PEN);
            var tp2 = tp.AgregarVenta(Dinero.Crear(12.50m, PEN));
            var tp3 = tp2.AgregarVenta(Dinero.Crear(7.50m, PEN));

            Assert.Multiple(() =>
            {
                Assert.That(tp2.MontoTotal.Monto, Is.EqualTo(12.50m));
                Assert.That(tp2.CantidadComprobantes, Is.EqualTo(1));

                Assert.That(tp3.MontoTotal.Monto, Is.EqualTo(20.00m));
                Assert.That(tp3.CantidadComprobantes, Is.EqualTo(2));
                Assert.That(tp3.Moneda, Is.EqualTo(PEN));
            });
        }

        [Test]
        public void AgregarVenta_MonedaDistinta_Lanza()
        {
            var tp = TicketPromedio.Vacio(PEN);
            Assert.Throws<InvalidOperationException>(() => tp.AgregarVenta(Dinero.Crear(5m, USD)));
        }

        [Test]
        public void QuitarVenta_RestaImporteYDecrementaContador()
        {
            var tp = TicketPromedio.Vacio(PEN)
                .AgregarVenta(Dinero.Crear(10m, PEN))
                .AgregarVenta(Dinero.Crear(5m, PEN));

            var tp2 = tp.QuitarVenta(Dinero.Crear(5m, PEN));

            Assert.Multiple(() =>
            {
                Assert.That(tp2.MontoTotal.Monto, Is.EqualTo(10m));
                Assert.That(tp2.CantidadComprobantes, Is.EqualTo(1));
            });
        }

        [Test]
        public void QuitarVenta_SinComprobantes_Lanza()
        {
            var tp = TicketPromedio.Vacio(PEN);
            Assert.Throws<InvalidOperationException>(() => tp.QuitarVenta(Dinero.Crear(1m, PEN)));
        }

        [Test]
        public void QuitarVenta_DejaCantidadEnCero_PeroTotalNoCero_Lanza()
        {
            // Estado actual: 1 comprobante, total 10
            var tp = TicketPromedio.Vacio(PEN).AgregarVenta(Dinero.Crear(10m, PEN));

            // Intento de quitar 9 (dejaría cantidad=0 y total=1) -> inconsistencia
            Assert.Throws<InvalidOperationException>(() => tp.QuitarVenta(Dinero.Crear(9m, PEN)));
        }

        [Test]
        public void QuitarVenta_DejaCantidadEnCero_YTotalCero_EsValido()
        {
            var tp = TicketPromedio.Vacio(PEN).AgregarVenta(Dinero.Crear(10m, PEN));
            var tp2 = tp.QuitarVenta(Dinero.Crear(10m, PEN));

            Assert.Multiple(() =>
            {
                Assert.That(tp2.CantidadComprobantes, Is.EqualTo(0));
                Assert.That(tp2.MontoTotal.Monto, Is.EqualTo(0m));
                Assert.That(tp2.Promedio.Monto, Is.EqualTo(0m));
            });
        }

        [Test]
        public void QuitarVenta_MonedaDistinta_Lanza()
        {
            var tp = TicketPromedio.Vacio(PEN).AgregarVenta(Dinero.Crear(10m, PEN));
            Assert.Throws<InvalidOperationException>(() => tp.QuitarVenta(Dinero.Crear(10m, USD)));
        }

        // -------- Combinar --------

        [Test]
        public void Combinar_SumaTotalesYCantidades_MismaMoneda()
        {
            var a = TicketPromedio.Crear(Dinero.Crear(30m, PEN), 3);
            var b = TicketPromedio.Crear(Dinero.Crear(20m, PEN), 2);

            var c = a.Combinar(b);

            Assert.Multiple(() =>
            {
                Assert.That(c.Moneda, Is.EqualTo(PEN));
                Assert.That(c.MontoTotal.Monto, Is.EqualTo(50m));
                Assert.That(c.CantidadComprobantes, Is.EqualTo(5));
                Assert.That(c.Promedio, Is.EqualTo(Dinero.Crear(10m, PEN)));
            });
        }

        [Test]
        public void Combinar_MonedaDistinta_Lanza()
        {
            var a = TicketPromedio.Crear(Dinero.Crear(30m, PEN), 3);
            var b = TicketPromedio.Crear(Dinero.Crear(20m, USD), 2);

            Assert.Throws<InvalidOperationException>(() => a.Combinar(b));
        }

        // -------- Igualdad / ToString --------

        [Test]
        public void Igualdad_PorValor()
        {
            var a = TicketPromedio.Crear(Dinero.Crear(15.00m, PEN), 3);
            var b = TicketPromedio.Crear(Dinero.Crear(15.0m, PEN), 3);

            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void ToString_ContieneDatosRelevantes()
        {
            var tp = TicketPromedio.Crear(Dinero.Crear(10m, PEN), 4);
            var s = tp.ToString();

            Assert.Multiple(() =>
            {
                Assert.That(s, Does.Contain("PEN"));
                Assert.That(s, Does.Contain("Total=10.00"));
                Assert.That(s, Does.Contain("Cant=4"));
                Assert.That(s, Does.Contain("Prom=2.50"));
            });
        }
    }
}
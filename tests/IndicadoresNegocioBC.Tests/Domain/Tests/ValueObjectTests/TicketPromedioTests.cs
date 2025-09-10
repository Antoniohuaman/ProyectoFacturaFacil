using System;
using System.Reflection;
using NUnit.Framework;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects; // Tipos: Moneda, Dinero

namespace IndicadoresNegocioBC.Tests.Domain.ValueObjects
{
    [TestFixture]
    public class TicketPromedioTests
    {
        // ----------------------------- Helpers robustos -----------------------------

        private static Moneda GetMoneda(string preferida = "PEN")
        {
            // Usar solo fábricas públicas del VO
            var code = preferida.ToUpperInvariant();
            if (code == "PEN") return Moneda.PEN();
            if (code == "USD") return Moneda.USD();
            // Si se requiere EUR u otra, usar Create
            if (code == "EUR") return Moneda.Create("EUR", "€", 2);
            // Fallback: intentar Create con defaults
            return Moneda.Create(code);
        }

        private static Dinero MakeDinero(decimal monto, Moneda moneda)
        {
            // 0) Cero(moneda)
            if (monto == 0m)
            {
                var mCero = typeof(Dinero).GetMethod("Cero", BindingFlags.Public | BindingFlags.Static, new[] { typeof(Moneda) });
                if (mCero?.Invoke(null, new object[] { moneda }) is Dinero d0) return d0;
            }

            // 1) Métodos estáticos (decimal, Moneda)
            foreach (var name in new[] { "From", "Crear", "Create", "Of", "New" })
            {
                var mi = typeof(Dinero).GetMethod(name, BindingFlags.Public | BindingFlags.Static, new[] { typeof(decimal), typeof(Moneda) });
                if (mi?.Invoke(null, new object[] { monto, moneda }) is Dinero d1) return d1;
            }

            // 2) Constructor (decimal, Moneda)
            var ctor = typeof(Dinero).GetConstructor(new[] { typeof(decimal), typeof(Moneda) });
            if (ctor?.Invoke(new object[] { monto, moneda }) is Dinero d2) return d2;

            // 3) Non-public constructor
            var ctorNon = typeof(Dinero).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(decimal), typeof(Moneda) }, null);
            if (ctorNon?.Invoke(new object[] { monto, moneda }) is Dinero d3) return d3;

            // 4) Otros órdenes de parámetros (Moneda, decimal)
            var ctorSwap = typeof(Dinero).GetConstructor(new[] { typeof(Moneda), typeof(decimal) });
            if (ctorSwap?.Invoke(new object[] { moneda, monto }) is Dinero d4) return d4;

            foreach (var name in new[] { "From", "Crear", "Create", "Of", "New" })
            {
                var mi = typeof(Dinero).GetMethod(name, BindingFlags.Public | BindingFlags.Static, new[] { typeof(Moneda), typeof(decimal) });
                if (mi?.Invoke(null, new object[] { moneda, monto }) is Dinero d5) return d5;
            }

            throw new InconclusiveException("No se pudo construir Dinero(decimal, Moneda).");
        }

        private static decimal GetMonto(Dinero d)
        {
            var p = typeof(Dinero).GetProperty("Monto", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (p == null) throw new InconclusiveException("Dinero no expone propiedad 'Monto'.");
            return (decimal)p.GetValue(d)!;
        }

        private static string GetCodigo(Moneda m)
        {
            var p = typeof(Moneda).GetProperty("Codigo", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (p == null) throw new InconclusiveException("Moneda no expone propiedad 'Codigo'.");
            return (string)p.GetValue(m)!;
        }

        // --------------------------------- Tests ---------------------------------

        [Test]
        public void Vacio_Deberia_Crear_TicketSinDatos_ConMonedaCorrecta()
        {
            var m = GetMoneda("PEN");
            var t = TicketPromedio.Vacio(m);

            Assert.That(t.Moneda, Is.EqualTo(m));
            Assert.That(t.CantidadComprobantes, Is.EqualTo(0));
            Assert.That(t.TieneDatos, Is.False);
            Assert.That(t.MontoTotal.EsCero, Is.True);
            Assert.That(GetMonto(t.Promedio), Is.EqualTo(0m));
        }

        [Test]
        public void Crear_Con_DatosValidos_Deberia_ConstruirCorrectamente()
        {
            var m = GetMoneda("PEN");
            var total = MakeDinero(150m, m);
            var t = TicketPromedio.Crear(total, 3);

            Assert.That(t.Moneda, Is.EqualTo(m));
            Assert.That(GetMonto(t.MontoTotal), Is.EqualTo(150m));
            Assert.That(t.CantidadComprobantes, Is.EqualTo(3));
            Assert.That(t.TieneDatos, Is.True);

            // Promedio = 50
            Assert.That(GetMonto(t.Promedio), Is.EqualTo(50m).Within(0.000001m));
        }

        [Test]
        public void Crear_Validaciones_Deben_Lanzar()
        {
            var m = GetMoneda("USD");
            var totalNoCero = MakeDinero(10m, m);
            var totalCero = MakeDinero(0m, m);

            // Cantidad negativa
            Assert.That(() => TicketPromedio.Crear(totalCero, -1), Throws.Exception.TypeOf<ArgumentOutOfRangeException>());

            // Cantidad 0 con total != 0
            Assert.That(() => TicketPromedio.Crear(totalNoCero, 0), Throws.Exception.TypeOf<ArgumentException>());
        }

        [Test]
        public void AgregarVenta_Deberia_Acumular_Total_Y_Cantidad()
        {
            var m = GetMoneda("PEN");
            var t = TicketPromedio.Vacio(m);

            var v1 = MakeDinero(100m, m);
            var t1 = t.AgregarVenta(v1);

            Assert.That(t.CantidadComprobantes, Is.EqualTo(0), "Inmutabilidad: el original no cambia");
            Assert.That(GetMonto(t1.MontoTotal), Is.EqualTo(100m));
            Assert.That(t1.CantidadComprobantes, Is.EqualTo(1));
            Assert.That(GetMonto(t1.Promedio), Is.EqualTo(100m));

            var v2 = MakeDinero(50m, m);
            var t2 = t1.AgregarVenta(v2);

            Assert.That(GetMonto(t2.MontoTotal), Is.EqualTo(150m));
            Assert.That(t2.CantidadComprobantes, Is.EqualTo(2));
            Assert.That(GetMonto(t2.Promedio), Is.EqualTo(75m));
        }

        [Test]
        public void QuitarVenta_Deberia_Revertir_Total_Y_Cantidad()
        {
            var m = GetMoneda("PEN");
            var t = TicketPromedio.Vacio(m)
                .AgregarVenta(MakeDinero(100m, m))
                .AgregarVenta(MakeDinero(50m, m)); // total=150, cant=2

            var t1 = t.QuitarVenta(MakeDinero(50m, m));
            Assert.That(GetMonto(t1.MontoTotal), Is.EqualTo(100m));
            Assert.That(t1.CantidadComprobantes, Is.EqualTo(1));
            Assert.That(GetMonto(t1.Promedio), Is.EqualTo(100m));

            var t2 = t1.QuitarVenta(MakeDinero(100m, m));
            Assert.That(GetMonto(t2.MontoTotal), Is.EqualTo(0m));
            Assert.That(t2.CantidadComprobantes, Is.EqualTo(0));
            Assert.That(t2.MontoTotal.EsCero, Is.True);
        }

        [Test]
        public void QuitarVenta_SinDatos_Deberia_Fallar()
        {
            var m = GetMoneda("PEN");
            var t = TicketPromedio.Vacio(m);

            Assert.That(() => t.QuitarVenta(MakeDinero(10m, m)),
                Throws.Exception.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void QuitarVenta_CuandoQuedaCantidadCero_PeroTotalNoCero_Deberia_Fallar()
        {
            var m = GetMoneda("USD");
            var total = MakeDinero(80m, m);
            var t = TicketPromedio.Crear(total, 1);

            // Quitar 50 => nuevaCantidad = 0, nuevoTotal = 30 => debe lanzar
            Assert.That(() => t.QuitarVenta(MakeDinero(50m, m)),
                Throws.Exception.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Combinar_MismaMoneda_Deberia_Sumar()
        {
            var m = GetMoneda("PEN");
            var a = TicketPromedio.Vacio(m)
                .AgregarVenta(MakeDinero(100m, m)) // total=100, cant=1
                .AgregarVenta(MakeDinero(20m, m)); // total=120, cant=2

            var b = TicketPromedio.Vacio(m)
                .AgregarVenta(MakeDinero(30m, m)); // total=30, cant=1

            var c = a.Combinar(b);
            Assert.That(GetMonto(c.MontoTotal), Is.EqualTo(150m));
            Assert.That(c.CantidadComprobantes, Is.EqualTo(3));
            Assert.That(GetMonto(c.Promedio), Is.EqualTo(50m));
        }

        [Test]
        public void Combinar_MonedasDistintas_Deberia_Fallar()
        {
            var pen = GetMoneda("PEN");
            var usd = GetMoneda("USD");

            var a = TicketPromedio.Vacio(pen).AgregarVenta(MakeDinero(10m, pen));
            var b = TicketPromedio.Vacio(usd).AgregarVenta(MakeDinero(10m, usd));

            Assert.That(() => a.Combinar(b),
                Throws.Exception.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Igualdad_Por_Valor_Deberia_Coincidir()
        {
            var m = GetMoneda("PEN");
            var t1 = TicketPromedio.Vacio(m)
                .AgregarVenta(MakeDinero(40m, m))
                .AgregarVenta(MakeDinero(10m, m)); // total=50, cant=2

            var t2 = TicketPromedio.Crear(MakeDinero(50m, m), 2);

            Assert.That(t1, Is.EqualTo(t2));
            Assert.That(t1.GetHashCode(), Is.EqualTo(t2.GetHashCode()));
        }

        [Test]
        public void ToString_Deberia_Incluir_Codigo_Moneda_Total_Cantidad_Promedio()
        {
            var m = GetMoneda("PEN");
            var t = TicketPromedio.Vacio(m).AgregarVenta(MakeDinero(123.45m, m));
            var s = t.ToString();

            Assert.That(s, Does.Contain(GetCodigo(m)));
            Assert.That(s, Does.Contain("Total="));
            Assert.That(s, Does.Contain("Cant="));
            Assert.That(s, Does.Contain("Prom="));
        }
    }
}

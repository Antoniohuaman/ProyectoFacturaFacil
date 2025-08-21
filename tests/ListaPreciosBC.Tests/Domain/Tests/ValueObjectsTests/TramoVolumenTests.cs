using System;
using System.Linq;
using NUnit.Framework;
using ListaPreciosBC.Domain.ValueObjects;
using SharedKernel.ValueObjects; // Dinero, Moneda

namespace ListaPreciosBC.Tests.ValueObjects
{
    [TestFixture]
    public class TramoVolumenTests
    {
        private static readonly Moneda PEN = Moneda.PEN();

        private static ValorPrecio VP(decimal monto, bool inc = true)
            => ValorPrecio.DesdeMonto(monto, PEN, inc);

        [Test]
        public void Crear_valido_cerrado_y_abierto_y_unitario()
        {
            var t1 = TramoVolumen.Cerrado(1, 10, VP(10));
            Assert.That(t1.MinCantidad, Is.EqualTo(1));
            Assert.That(t1.MaxCantidad, Is.EqualTo(10));
            Assert.That(t1.Precio.Importe.Monto, Is.EqualTo(10m));

            var t2 = TramoVolumen.Desde(11, VP(8));
            Assert.That(t2.MinCantidad, Is.EqualTo(11));
            Assert.That(t2.MaxCantidad, Is.Null);

            var t3 = TramoVolumen.Unitario(5, VP(9.99m));
            Assert.That(t3.MinCantidad, Is.EqualTo(5));
            Assert.That(t3.MaxCantidad, Is.EqualTo(5));
        }

        [Test]
        public void Crear_invalido_lanza_en_min_y_max_y_precio_nulo()
        {
            Assert.That(() => TramoVolumen.Crear(0, 10, VP(10)), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => TramoVolumen.Crear(10, 9, VP(10)), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => TramoVolumen.Crear(1, 10, null!), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void TryCrear_true_para_valido_false_para_invalido()
        {
            Assert.That(TramoVolumen.TryCrear(1, 10, VP(10), out var ok), Is.True);
            Assert.That(ok, Is.Not.Null);

            Assert.That(TramoVolumen.TryCrear(10, 9, VP(10), out var bad1), Is.False);
            Assert.That(bad1, Is.Null);

            Assert.That(TramoVolumen.TryCrear(1, 10, null, out var bad2), Is.False);
            Assert.That(bad2, Is.Null);
        }

        [Test]
        public void ContieneCantidad_respeta_bordes_y_abierto()
        {
            var t = TramoVolumen.Cerrado(5, 10, VP(10));

            Assert.That(t.ContieneCantidad(4), Is.False);
            Assert.That(t.ContieneCantidad(5), Is.True);
            Assert.That(t.ContieneCantidad(10), Is.True);
            Assert.That(t.ContieneCantidad(11), Is.False);

            var abierto = TramoVolumen.Desde(11, VP(8));
            Assert.That(abierto.ContieneCantidad(1000), Is.True);
        }

        [Test]
        public void SeSuperponeCon_detecta_solapes_inclusivos_y_con_abiertos()
        {
            var a = TramoVolumen.Cerrado(1, 10, VP(10));
            var b = TramoVolumen.Cerrado(5, 12, VP(9));
            var c = TramoVolumen.Cerrado(11, 20, VP(8));
            var d = TramoVolumen.Desde(10, VP(7));

            Assert.That(a.SeSuperponeCon(b), Is.True);   // 1..10 with 5..12
            Assert.That(a.SeSuperponeCon(c), Is.False);  // 1..10 with 11..20 (contiguos, no solape)
            Assert.That(a.SeSuperponeCon(d), Is.True);   // 1..10 with 10..∞ (comparten 10)
            Assert.That(c.SeSuperponeCon(d), Is.True);   // 11..20 with 10..∞
        }

        [Test]
        public void EsContiguoCon_detecta_adyacencia_sin_solape()
        {
            var a = TramoVolumen.Cerrado(1, 10, VP(10));
            var b = TramoVolumen.Cerrado(11, 20, VP(8));
            var c = TramoVolumen.Desde(21, VP(7));
            var d = TramoVolumen.Desde(10, VP(9)); // abierto desde 10

            Assert.That(a.EsContiguoCon(b), Is.True);    // 1..10 y 11..20
            Assert.That(b.EsContiguoCon(c), Is.True);    // 11..20 y 21..∞
            Assert.That(a.EsContiguoCon(d), Is.False);   // contigüidad requiere límite superior finito en 'a' o 'd'
        }

        [Test]
        public void ConPrecio_devuelve_nueva_instancia_con_mismo_rango()
        {
            var a = TramoVolumen.Cerrado(1, 10, VP(10));
            var b = a.ConPrecio(VP(9.5m, inc:false));

            Assert.That(b.MinCantidad, Is.EqualTo(1));
            Assert.That(b.MaxCantidad, Is.EqualTo(10));
            Assert.That(b.Precio.Importe.Monto, Is.EqualTo(9.5m));
            Assert.That(b.Precio.IncluyeImpuesto, Is.False);
            Assert.That(a, Is.Not.EqualTo(b)); // precio cambió
        }

        [Test]
        public void Igualdad_y_hashcode_por_valor()
        {
            var x1 = TramoVolumen.Cerrado(1, 10, VP(10));
            var x2 = TramoVolumen.Cerrado(1, 10, VP(10));
            var y  = TramoVolumen.Cerrado(1, 10, VP(9));
            var z  = TramoVolumen.Cerrado(2, 10, VP(10));

            Assert.That(x1, Is.EqualTo(x2));
            Assert.That(x1.GetHashCode(), Is.EqualTo(x2.GetHashCode()));
            Assert.That(x1.Equals(y), Is.False); // precio distinto
            Assert.That(x1.Equals(z), Is.False); // rango distinto
        }

        [Test]
        public void CompareTo_orden_por_min_luego_max_null_al_final()
        {
            var a = TramoVolumen.Cerrado(1, 10, VP(10));
            var b = TramoVolumen.Cerrado(5, 12, VP(9));
            var c = TramoVolumen.Desde(13, VP(8));
            var d = TramoVolumen.Cerrado(13, 50, VP(7));

            var arr = new[] { c, a, d, b }.OrderBy(t => t).ToArray();

            // Orden esperado: a(1..10), b(5..12), d(13..50), c(13..∞)
            Assert.That(arr[0], Is.EqualTo(a));
            Assert.That(arr[1], Is.EqualTo(b));
            Assert.That(arr[2], Is.EqualTo(d));
            Assert.That(arr[3], Is.EqualTo(c));
        }

        [Test]
        public void ToString_formatea_legible_con_infinito()
        {
            var a = TramoVolumen.Cerrado(1, 10, VP(10));
            var b = TramoVolumen.Desde(11, VP(8));
            Assert.That(a.ToString(), Does.StartWith("[1..10]"));
            Assert.That(b.ToString(), Does.StartWith("[11..∞]"));
        }
    }
}

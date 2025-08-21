using System;
using System.Linq;
using NUnit.Framework;
using ListaPreciosBC.Domain.ValueObjects;
using SharedKernel.ValueObjects; // Dinero, Moneda
using System.Collections.Generic;

namespace ListaPreciosBC.Tests.ValueObjects
{
    [TestFixture]
    public class MatrizVolumenTests
    {
        private static readonly Moneda PEN = Moneda.PEN();

        private static ValorPrecio VP(decimal monto, bool inc = true)
            => ValorPrecio.DesdeMonto(monto, PEN, inc);

        private static TramoVolumen T(int min, int? max, decimal precio)
            => TramoVolumen.Crear(min, max, VP(precio));

        [Test]
        public void Crear_ordenando_y_validando_solapes_y_consistencia()
        {
            var insumos = new[]
            {
                T(11, 20, 8m),
                T(1, 10, 10m),
                T(21, null, 5m)
            };

            var m = MatrizVolumen.Crear(insumos);

            Assert.That(m.Count, Is.EqualTo(3));
            Assert.That(m.Tramos[0].MinCantidad, Is.EqualTo(1));
            Assert.That(m.Tramos[2].MaxCantidad, Is.Null);

            // Moneda/flag consistentes permiten exponer propiedades
            Assert.That(m.Moneda, Is.EqualTo(PEN));
            Assert.That(m.IncluyeImpuesto, Is.True);
        }

        [Test]
        public void Crear_lanza_si_hay_solapes_o_inconsistencias()
        {
            // Solape: 1..10 y 10..20 (solape en 10 si el 2º empieza en 10)
            var solape = new[] { T(1, 10, 10m), T(10, 20, 9m) };
            Assert.That(() => MatrizVolumen.Crear(solape),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("Solape"));

            // Moneda inconsistente
            var otroMon = ValorPrecio.DesdeMonto(9m, Moneda.USD(), true);
            var inconsMoneda = new[] { T(1, 10, 10m), TramoVolumen.Crear(11, 20, otroMon) };
            Assert.That(() => MatrizVolumen.Crear(inconsMoneda),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("Moneda"));

            // Flag inconsistente
            var incFlag = new[] { T(1, 10, 10m), TramoVolumen.Crear(11, 20, VP(9m, inc:false)) };
            Assert.That(() => MatrizVolumen.Crear(incFlag),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("IncluyeImpuesto"));
        }

        [Test]
        public void Crear_con_continuidad_desde_uno_exige_cobertura_sin_huecos()
        {
            // Falta iniciar en 1
            var a = new[] { T(2, 10, 10m) };
            Assert.That(() => MatrizVolumen.Crear(a, exigirContinuidadDesdeUno: true),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("cantidad 1"));

            // Hueco entre tramos
            var b = new[] { T(1, 10, 10m), T(12, 20, 9m) };
            Assert.That(() => MatrizVolumen.Crear(b, exigirContinuidadDesdeUno: true),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("Hueco"));

            // Abierto en mitad + tramos posteriores no permitido
            var c = new[] { T(1, 10, 10m), T(11, null, 8m), T(100, null, 7m) };
            Assert.That(() => MatrizVolumen.Crear(c, exigirContinuidadDesdeUno: true),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("Solape"));
        }

        [Test]
        public void Crear_colapsa_contiguos_con_mismo_precio_si_se_solicita()
        {
            var insumos = new[]
            {
                T(1, 10, 10m),
                T(11, 20, 10m), // contiguo y mismo precio -> colapsa a [1..20]
                T(21, null, 8m)
            };

            var m = MatrizVolumen.Crear(insumos, colapsarContiguosIgualPrecio: true);
            Assert.That(m.Count, Is.EqualTo(2));
            Assert.That(m.Tramos[0].MinCantidad, Is.EqualTo(1));
            Assert.That(m.Tramos[0].MaxCantidad, Is.EqualTo(20));
            Assert.That(m.Tramos[1].MinCantidad, Is.EqualTo(21));
            Assert.That(m.Tramos[1].MaxCantidad, Is.Null);
        }

        [Test]
        public void ObtenerTramo_devuelve_el_correcto_o_null()
        {
            var m = MatrizVolumen.Crear(new[] { T(1, 10, 10m), T(11, 20, 8m), T(21, null, 5m) });

            Assert.That(m.ObtenerTramo(1)!.Precio.Importe.Monto, Is.EqualTo(10m));
            Assert.That(m.ObtenerTramo(15)!.Precio.Importe.Monto, Is.EqualTo(8m));
            Assert.That(m.ObtenerTramo(100)!.Precio.Importe.Monto, Is.EqualTo(5m));
            Assert.That(m.ObtenerTramo(0), Is.Null);
            Assert.That(m.ObtenerTramo(-5), Is.Null);
        }

        [Test]
        public void Insertar_inserta_en_orden_y_colapsa_si_corresponde()
        {
            var m = MatrizVolumen.Crear(new[] { T(1, 10, 10m), T(21, null, 5m) });
            var m2 = m.Insertar(T(11, 20, 10m)); // contiguo a [1..10] y mismo precio -> colapso a [1..20]

            Assert.That(m2.Count, Is.EqualTo(2));
            Assert.That(m2.Tramos[0].MinCantidad, Is.EqualTo(1));
            Assert.That(m2.Tramos[0].MaxCantidad, Is.EqualTo(20));
            Assert.That(m2.Tramos[1].MinCantidad, Is.EqualTo(21));
            Assert.That(m2.Tramos[1].MaxCantidad, Is.Null);
        }

        [Test]
        public void Insertar_lanza_si_hay_solape_o_inconsistencia()
        {
            var m = MatrizVolumen.Crear(new[] { T(1, 10, 10m), T(21, null, 5m) });

            // Solape con 1..10
            Assert.That(() => m.Insertar(T(10, 15, 9m)),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("Solape"));

            // Moneda inconsistente
            var otroMon = TramoVolumen.Crear(11, 20, ValorPrecio.DesdeMonto(9m, Moneda.USD(), true));
            Assert.That(() => m.Insertar(otroMon),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("Moneda"));

            // Flag inconsistente
            var otroFlag = TramoVolumen.Crear(11, 20, ValorPrecio.DesdeMonto(9m, PEN, false));
            Assert.That(() => m.Insertar(otroFlag),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("IncluyeImpuesto"));
        }

        [Test]
        public void Reemplazar_actualiza_rango_existente_y_valida_solapes()
        {
            var m = MatrizVolumen.Crear(new[] { T(1, 10, 10m), T(11, 20, 8m), T(21, null, 5m) });

            var existente = T(11, 20, 8m);
            var nuevo = T(11, 20, 7.5m);

            var m2 = m.Reemplazar(existente, nuevo);
            Assert.That(m2.Count, Is.EqualTo(3));
            Assert.That(m2.Tramos[1].Precio.Importe.Monto, Is.EqualTo(7.5m));

            // Reemplazar a un rango que cause solape
            var malo = T(9, 22, 9m);
            Assert.That(() => m.Reemplazar(existente, malo),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("Solape"));

            // Rango inexistente
            var inexistente = T(5, 9, 10m);
            Assert.That(() => m.Reemplazar(inexistente, nuevo),
                Throws.TypeOf<KeyNotFoundException>());
        }

        [Test]
        public void Eliminar_quita_por_rango_y_devuelve_nueva_instancia()
        {
            var m = MatrizVolumen.Crear(new[] { T(1, 10, 10m), T(11, 20, 8m), T(21, null, 5m) });

            var m2 = m.Eliminar(T(11, 20, 8m));
            Assert.That(m2.Count, Is.EqualTo(2));
            Assert.That(m.Count, Is.EqualTo(3)); // inmutabilidad

            Assert.That(() => m.Eliminar(T(100, 200, 1m)), Throws.TypeOf<KeyNotFoundException>());
        }

        [Test]
        public void Igualdad_y_hashcode_consideran_la_secuencia_de_tramos()
        {
            var a = MatrizVolumen.Crear(new[] { T(1, 10, 10m), T(11, 20, 8m) });
            var b = MatrizVolumen.Crear(new[] { T(1, 10, 10m), T(11, 20, 8m) });
            var c = MatrizVolumen.Crear(new[] { T(1, 10, 10m), T(11, 20, 7m) });

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            Assert.That(a.Equals(c), Is.False);
        }

        [Test]
        public void ToString_concatenado_legible()
        {
            var m = MatrizVolumen.Crear(new[] { T(1, 10, 10m), T(11, null, 8m) });
            var s = m.ToString();
            Assert.That(s, Does.Contain("[1..10]"));
            Assert.That(s, Does.Contain("[11..∞]"));
            Assert.That(s, Does.Contain("=>"));
            Assert.That(s, Does.Contain("|"));
        }
    }
}
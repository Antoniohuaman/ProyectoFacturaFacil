using System;
using System.Linq;
using NUnit.Framework;
using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Tests.UnitTests.ValueObjects
{
    [TestFixture]
    public class IdentificadorColumnaPrecioTests
    {
        [Test]
        public void Crear_valido_normaliza_mayusculas_y_trim()
        {
            var id = IdentificadorColumnaPrecio.Crear("  p5  ");
            Assert.That(id.Valor, Is.EqualTo("P5"));
            Assert.That(id.Numero, Is.EqualTo(5));
        }

        [Test]
        public void Crear_invalido_lanza_excepcion_adecuada()
        {
            var invalidos = new[]
            {
                "", "   ", "P0", "P11", "P01", "Q1", "1", "PX", "P-1", "PP1", "P 3"
            };

            foreach (var s in invalidos)
            {
                Assert.That(() => IdentificadorColumnaPrecio.Crear(s),
                    Throws.Exception.TypeOf<ArgumentOutOfRangeException>(),
                    $"Esperaba excepción para '{s}'");
            }

            Assert.That(() => IdentificadorColumnaPrecio.Crear(null!), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void DesdeNumero_valido_devuelve_instancias_correctas()
        {
            for (byte n = IdentificadorColumnaPrecio.Min; n <= IdentificadorColumnaPrecio.Max; n++)
            {
                var id = IdentificadorColumnaPrecio.DesdeNumero(n);
                Assert.That(id.Numero, Is.EqualTo(n));
                Assert.That(id.Valor, Is.EqualTo($"P{n}"));
            }
        }

        [Test]
        public void DesdeNumero_fuera_de_rango_lanza()
        {
            Assert.That(() => IdentificadorColumnaPrecio.DesdeNumero(0),  Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => IdentificadorColumnaPrecio.DesdeNumero(11), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TryCrear_devuelve_false_en_invalidos_y_true_en_validos()
        {
            Assert.That(IdentificadorColumnaPrecio.TryCrear("p10", out var ok), Is.True);
            Assert.That(ok, Is.Not.Null);
            Assert.That(ok!.Valor, Is.EqualTo("P10"));
            Assert.That(ok.Numero, Is.EqualTo(10));

            Assert.That(IdentificadorColumnaPrecio.TryCrear("P11", out var bad), Is.False);
            Assert.That(bad, Is.Null);

            Assert.That(IdentificadorColumnaPrecio.TryCrear(null, out var nulo), Is.False);
            Assert.That(nulo, Is.Null);
        }

        [Test]
        public void TryDesdeNumero_valida_rango_sin_excepciones()
        {
            Assert.That(IdentificadorColumnaPrecio.TryDesdeNumero(1, out var p1), Is.True);
            Assert.That(p1, Is.Not.Null);
            Assert.That(p1!.Valor, Is.EqualTo("P1"));

            Assert.That(IdentificadorColumnaPrecio.TryDesdeNumero(0, out var p0), Is.False);
            Assert.That(p0, Is.Null);
        }

        [Test]
        public void Igualdad_por_valor_y_hashcode_consistente()
        {
            var a = IdentificadorColumnaPrecio.Crear("P3");
            var b = IdentificadorColumnaPrecio.Crear("p3");
            var c = IdentificadorColumnaPrecio.Crear("P4");

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a == b, Is.True);
            Assert.That(a != b, Is.False);
            Assert.That(a.Equals(c), Is.False);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void Orden_natural_por_numero_funciona_para_sort()
        {
            var p3 = IdentificadorColumnaPrecio.Crear("P3");
            var p1 = IdentificadorColumnaPrecio.Crear("P1");
            var p10 = IdentificadorColumnaPrecio.Crear("P10");

            var arr = new[] { p3, p1, p10 }.OrderBy(x => x).ToArray();
            Assert.That(arr.Select(x => x.Valor), Is.EqualTo(new[] { "P1", "P3", "P10" }));
        }

        [Test]
        public void Todos_devuelve_P1_a_P10_en_orden_y_sin_repetidos()
        {
            var all = IdentificadorColumnaPrecio.Todos;

            Assert.That(all.Count, Is.EqualTo(10));
            Assert.That(all.First().Valor, Is.EqualTo("P1"));
            Assert.That(all.Last().Valor,  Is.EqualTo("P10"));

            // 1..10
            Assert.That(all.Select(x => x.Numero), Is.EqualTo(Enumerable.Range(1, 10)));

            // Sin duplicados
            Assert.That(all.Select(x => x.Valor).Distinct().Count(), Is.EqualTo(10));
        }

        [Test]
        public void ToString_devuelve_el_valor_normalizado()
        {
            var id = IdentificadorColumnaPrecio.Crear("p7");
            Assert.That(id.ToString(), Is.EqualTo("P7"));
        }
    }
}

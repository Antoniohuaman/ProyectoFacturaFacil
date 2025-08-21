using System;
using System.Linq;
using NUnit.Framework;
using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Tests.ValueObjects
{
    [TestFixture]
    public class NombreColumnaPrecioTests
    {
        [Test]
        public void Crear_normaliza_trim_y_colapsa_espacios()
        {
            var n = NombreColumnaPrecio.Crear("   Precio   VIP\t  especial  ");
            Assert.That(n.Valor, Is.EqualTo("Precio VIP especial"));
        }

        [Test]
        public void Crear_preserva_mayus_minus_y_acentos_pero_igualdad_es_case_insensitive()
        {
            var a = NombreColumnaPrecio.Crear("Precio mayorista");
            var b = NombreColumnaPrecio.Crear("precio MAYORISTA");
            var c = NombreColumnaPrecio.Crear("Precio promoción");

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.Equals(c), Is.False);
            Assert.That(c.Valor, Is.EqualTo("Precio promoción")); // acentos conservados
        }

        [Test]
        public void Crear_invalido_vacio_lanza()
        {
            Assert.That(() => NombreColumnaPrecio.Crear(""), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => NombreColumnaPrecio.Crear("   "), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => NombreColumnaPrecio.Crear(null!), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Crear_invalido_excede_max_longitud_lanza()
        {
            var largo31 = new string('A', NombreColumnaPrecio.MaxLongitud + 1);
            Assert.That(() => NombreColumnaPrecio.Crear(largo31), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Crear_invalido_contiene_control_lanza()
        {
            var conControl = "Precio\u0001VIP"; // U+0001 (char de control)
            Assert.That(() => NombreColumnaPrecio.Crear(conControl), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TryCrear_devuelve_true_con_texto_valido_y_false_en_invalidos()
        {
            Assert.That(NombreColumnaPrecio.TryCrear("  Distribuidor  ", out var ok), Is.True);
            Assert.That(ok, Is.Not.Null);
            Assert.That(ok!.Valor, Is.EqualTo("Distribuidor"));

            Assert.That(NombreColumnaPrecio.TryCrear("   ", out var vacio), Is.False);
            Assert.That(vacio, Is.Null);

            Assert.That(NombreColumnaPrecio.TryCrear(null, out var nulo), Is.False);
            Assert.That(nulo, Is.Null);
        }

        [Test]
        public void Igualdad_por_valor_y_hashcode_case_insensitive()
        {
            var x = NombreColumnaPrecio.Crear("Promoción");
            var y = NombreColumnaPrecio.Crear("PROMOCIÓN");
            var z = NombreColumnaPrecio.Crear("VIP");

            Assert.That(x, Is.EqualTo(y));
            Assert.That(x.GetHashCode(), Is.EqualTo(y.GetHashCode()));
            Assert.That(x.Equals(z), Is.False);
        }

        [Test]
        public void CompareTo_orden_alfabetico_case_insensitive()
        {
            var a = NombreColumnaPrecio.Crear("VIP");
            var b = NombreColumnaPrecio.Crear("distribuidor");
            var c = NombreColumnaPrecio.Crear("Mayorista");

            var sorted = new[] { a, b, c }.OrderBy(n => n).ToArray();
            Assert.That(sorted.Select(n => n.Valor), Is.EqualTo(new[] { "distribuidor", "Mayorista", "VIP" }));
        }

        [Test]
        public void ToString_devuelve_el_valor_normalizado()
        {
            var n = NombreColumnaPrecio.Crear("   Precio   base   ");
            Assert.That(n.ToString(), Is.EqualTo("Precio base"));
        }

        [Test]
        public void Conversion_explicit_a_string_devuelve_valor()
        {
            var n = NombreColumnaPrecio.Crear("VIP");
            var s = (string)n;
            Assert.That(s, Is.EqualTo("VIP"));
        }

        [Test]
        public void Longitud_minima_y_maxima_expuestas_para_validaciones_de_ui()
        {
            Assert.That(NombreColumnaPrecio.MinLongitud, Is.EqualTo(1));
            Assert.That(NombreColumnaPrecio.MaxLongitud, Is.EqualTo(30));
        }
    }
}

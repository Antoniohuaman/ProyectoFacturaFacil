using System;
using System.Linq;
using NUnit.Framework;
using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Tests.ValueObjects
{
    [TestFixture]
    public class ModoValorizacionColumnaTests
    {
        [Test]
        public void Instancias_publicas_son_unicas_y_correctas()
        {
            Assert.That(ModoValorizacionColumna.Fijo.Codigo, Is.EqualTo("F"));
            Assert.That(ModoValorizacionColumna.Fijo.Nombre, Is.EqualTo("Fijo"));
            Assert.That(ModoValorizacionColumna.PorVolumen.Codigo, Is.EqualTo("V"));
            Assert.That(ModoValorizacionColumna.PorVolumen.Nombre, Is.EqualTo("PorVolumen"));

            // unicidad (mismas referencias)
            Assert.That(ReferenceEquals(ModoValorizacionColumna.Fijo, ModoValorizacionColumna.Fijo), Is.True);
            Assert.That(ReferenceEquals(ModoValorizacionColumna.PorVolumen, ModoValorizacionColumna.PorVolumen), Is.True);
        }

        [Test]
        public void Todos_devuelve_en_orden_natural()
        {
            var all = ModoValorizacionColumna.Todos;
            Assert.That(all.Count, Is.EqualTo(2));
            Assert.That(all[0], Is.EqualTo(ModoValorizacionColumna.Fijo));
            Assert.That(all[1], Is.EqualTo(ModoValorizacionColumna.PorVolumen));
        }

        [Test]
        public void Crear_desde_texto_admite_nombres_y_codigos_con_variantes()
        {
            Assert.That(ModoValorizacionColumna.Crear("Fijo"), Is.EqualTo(ModoValorizacionColumna.Fijo));
            Assert.That(ModoValorizacionColumna.Crear(" fijo "), Is.EqualTo(ModoValorizacionColumna.Fijo));
            Assert.That(ModoValorizacionColumna.Crear("F"), Is.EqualTo(ModoValorizacionColumna.Fijo));

            Assert.That(ModoValorizacionColumna.Crear("Por Volumen"), Is.EqualTo(ModoValorizacionColumna.PorVolumen));
            Assert.That(ModoValorizacionColumna.Crear("POR_VOLUMEN"), Is.EqualTo(ModoValorizacionColumna.PorVolumen));
            Assert.That(ModoValorizacionColumna.Crear("por-volumen"), Is.EqualTo(ModoValorizacionColumna.PorVolumen));
            Assert.That(ModoValorizacionColumna.Crear("V"), Is.EqualTo(ModoValorizacionColumna.PorVolumen));
            Assert.That(ModoValorizacionColumna.Crear("PV"), Is.EqualTo(ModoValorizacionColumna.PorVolumen));
        }

        [Test]
        public void Crear_invalido_lanza_argument_out_of_range()
        {
            var invalidos = new[] { "", "   ", "X", "VOL", "POR", "FI", "FIJOX", "VOLUMEN" };
            foreach (var s in invalidos)
            {
                Assert.That(() => ModoValorizacionColumna.Crear(s),
                    Throws.Exception.TypeOf<ArgumentOutOfRangeException>(),
                    $"Esperaba excepción para '{s}'");
            }

            Assert.That(() => ModoValorizacionColumna.Crear(null!), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void TryCrear_true_para_validos_false_para_invalidos()
        {
            Assert.That(ModoValorizacionColumna.TryCrear("Fijo", out var fijo), Is.True);
            Assert.That(fijo, Is.EqualTo(ModoValorizacionColumna.Fijo));

            Assert.That(ModoValorizacionColumna.TryCrear("PV", out var vol), Is.True);
            Assert.That(vol, Is.EqualTo(ModoValorizacionColumna.PorVolumen));

            Assert.That(ModoValorizacionColumna.TryCrear("xxx", out var bad), Is.False);
            Assert.That(bad, Is.Null);

            Assert.That(ModoValorizacionColumna.TryCrear(null, out var nulo), Is.False);
            Assert.That(nulo, Is.Null);
        }

        [Test]
        public void DesdeCodigo_equivale_a_Crear_y_TryDesdeCodigo_funciona()
        {
            var a = ModoValorizacionColumna.DesdeCodigo("F");
            Assert.That(a, Is.EqualTo(ModoValorizacionColumna.Fijo));

            Assert.That(ModoValorizacionColumna.TryDesdeCodigo("V", out var b), Is.True);
            Assert.That(b, Is.EqualTo(ModoValorizacionColumna.PorVolumen));

            Assert.That(ModoValorizacionColumna.TryDesdeCodigo("Z", out var c), Is.False);
            Assert.That(c, Is.Null);
        }

        [Test]
        public void Igualdad_y_hashcode_por_valor()
        {
            var x = ModoValorizacionColumna.Crear("F");
            var y = ModoValorizacionColumna.Crear("fijo");
            var z = ModoValorizacionColumna.Crear("PV");

            Assert.That(x, Is.EqualTo(ModoValorizacionColumna.Fijo));
            Assert.That(x, Is.EqualTo(y));
            Assert.That(x.GetHashCode(), Is.EqualTo(y.GetHashCode()));
            Assert.That(z, Is.EqualTo(ModoValorizacionColumna.PorVolumen));
            Assert.That(x == y, Is.True);
            Assert.That(x != z, Is.True);
        }

        [Test]
        public void CompareTo_sigue_el_orden_natural_Fijo_antes_que_PorVolumen()
        {
            var arr = new[] { ModoValorizacionColumna.PorVolumen, ModoValorizacionColumna.Fijo }
                      .OrderBy(m => m).ToArray();

            Assert.That(arr[0], Is.EqualTo(ModoValorizacionColumna.Fijo));
            Assert.That(arr[1], Is.EqualTo(ModoValorizacionColumna.PorVolumen));
        }

        [Test]
        public void Helpers_EsFijo_y_EsPorVolumen()
        {
            Assert.That(ModoValorizacionColumna.Fijo.EsFijo, Is.True);
            Assert.That(ModoValorizacionColumna.Fijo.EsPorVolumen, Is.False);

            Assert.That(ModoValorizacionColumna.PorVolumen.EsPorVolumen, Is.True);
            Assert.That(ModoValorizacionColumna.PorVolumen.EsFijo, Is.False);
        }

        [Test]
        public void ToString_devuelve_el_nombre_legible()
        {
            Assert.That(ModoValorizacionColumna.Fijo.ToString(), Is.EqualTo("Fijo"));
            Assert.That(ModoValorizacionColumna.PorVolumen.ToString(), Is.EqualTo("PorVolumen"));
        }
    }
}

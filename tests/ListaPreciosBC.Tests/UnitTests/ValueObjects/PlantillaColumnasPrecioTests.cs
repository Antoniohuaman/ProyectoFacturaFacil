using System;
using System.Linq;
using NUnit.Framework;
using ListaPreciosBC.Domain.ValueObjects;
using System.Collections.Generic; 

namespace ListaPreciosBC.Tests.ValueObjects
{
    [TestFixture]
    public class PlantillaColumnasPrecioTests
    {
        private static IdentificadorColumnaPrecio P(byte n) => IdentificadorColumnaPrecio.DesdeNumero(n);
        private static NombreColumnaPrecio N(string s) => NombreColumnaPrecio.Crear(s);
        private static ModoValorizacionColumna Fijo => ModoValorizacionColumna.Fijo;
        private static ModoValorizacionColumna Vol  => ModoValorizacionColumna.PorVolumen;

        private static ConfiguracionColumnaPrecio Cfg(byte p, string nombre, bool baseCol = false, bool visible = true, byte? orden = null, bool vol = false)
            => ConfiguracionColumnaPrecio.Crear(P(p), N(nombre), vol ? Vol : Fijo, baseCol, visible, orden);

        private static PlantillaColumnasPrecio PlantillaBasica()
            => PlantillaColumnasPrecio.Crear(new[]
            {
                Cfg(1, "Precio base", baseCol:true,  orden:1),
                Cfg(2, "Mayorista",   orden:2),
                Cfg(3, "Distrib.",    orden:3, vol:true)
            });

        [Test]
        public void Crear_valida_invariantes_y_ordena_por_orden()
        {
            var p = PlantillaBasica();

            Assert.That(p.Count, Is.EqualTo(3));
            Assert.That(p.Base.Id.Numero, Is.EqualTo(1));
            Assert.That(p.Columnas.Select(c => c.Orden), Is.EqualTo(new byte[] {1,2,3}));
        }

        [Test]
        public void Crear_falla_en_casos_incorrectos()
        {
            // sin base
            var sinBase = new[]
            {
                Cfg(1, "P1", baseCol:false, orden:1),
                Cfg(2, "P2", baseCol:false, orden:2)
            };
            Assert.That(() => PlantillaColumnasPrecio.Crear(sinBase),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("Base"));

            // múltiples base
            var multiBase = new[]
            {
                Cfg(1, "P1", baseCol:true,  orden:1),
                Cfg(2, "P2", baseCol:true,  orden:2)
            };
            Assert.That(() => PlantillaColumnasPrecio.Crear(multiBase),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("Base"));

            // orden duplicado
            var ordenDup = new[]
            {
                Cfg(1, "P1", baseCol:true,  orden:1),
                Cfg(2, "P2", baseCol:false, orden:1)
            };
            Assert.That(() => PlantillaColumnasPrecio.Crear(ordenDup),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("órdenes"));

            // id duplicado
            var idDup = new[]
            {
                Cfg(1, "P1", baseCol:true, orden:1),
                Cfg(1, "P1-bis", orden:2)
            };
            Assert.That(() => PlantillaColumnasPrecio.Crear(idDup),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("IDs"));

            // sin visibles
            var sinVisible = new[]
            {
                Cfg(1, "P1", baseCol:true,  visible:false, orden:1),
                Cfg(2, "P2", baseCol:false, visible:false, orden:2)
            };
            Assert.That(() => PlantillaColumnasPrecio.Crear(sinVisible),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("visible"));

            // cantidad > 10
            var once = Enumerable.Range(1, 11)
                .Select(i => Cfg((byte)Math.Min(i,10), $"P{i}", baseCol:(i==1), orden:(byte)Math.Min(i,10)))
                .ToArray();
            // (ajustamos para que no doble Id) forzamos 11 columnas con Id repetidos
            Assert.That(() => PlantillaColumnasPrecio.Crear(once),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("entre 1 y 10"));
        }

        [Test]
        public void TryCrear_true_en_valido_false_en_invalido()
        {
            Assert.That(PlantillaColumnasPrecio.TryCrear(new[] { Cfg(1,"Base",true, orden:1) }, out var ok), Is.True);
            Assert.That(ok, Is.Not.Null);

            Assert.That(PlantillaColumnasPrecio.TryCrear(new ConfiguracionColumnaPrecio[0], out var bad), Is.False);
            Assert.That(bad, Is.Null);
        }

        [Test]
        public void Obtener_y_Base_funcionan()
        {
            var p = PlantillaBasica();

            var c2 = p.Obtener(P(2));
            Assert.That(c2.Nombre.Valor, Is.EqualTo("Mayorista"));

            Assert.That(p.Base.Id.Numero, Is.EqualTo(1));
        }

        [Test]
        public void Renombrar_y_CambiarModo_reemplazan_inmutablemente()
        {
            var p = PlantillaBasica();

            var p2 = p.Renombrar(P(2), N("Mayorista Plus"));
            Assert.That(p2.Obtener(P(2)).Nombre.Valor, Is.EqualTo("Mayorista Plus"));
            Assert.That(p.Obtener(P(2)).Nombre.Valor,  Is.EqualTo("Mayorista")); // inmutabilidad

            var p3 = p.CambiarModo(P(3), Fijo);
            Assert.That(p3.Obtener(P(3)).Modo, Is.EqualTo(Fijo));
            Assert.That(p.Obtener(P(3)).Modo,  Is.EqualTo(Vol));
        }

        [Test]
        public void MarcarComoBase_desmarca_anteriores_y_garantiza_unicidad()
        {
            var p = PlantillaBasica();
            var p2 = p.MarcarComoBase(P(2));

            Assert.That(p2.Base.Id.Numero, Is.EqualTo(2));
            Assert.That(p2.Obtener(P(1)).EsBase, Is.False);
        }

        [Test]
    public void Mostrar_y_Ocultar_respetan_regla_de_al_menos_una_visible()
    {
    var p = PlantillaColumnasPrecio.Crear(new[]
    {
        Cfg(1,"Base", baseCol:true,  visible:true,  orden:1),
        Cfg(2,"Aux",  baseCol:false, visible:false, orden:2),
    });

    // Debe lanzar porque P1 es la única visible
    Assert.That(() => p.Ocultar(P(1)), Throws.TypeOf<InvalidOperationException>());

    // Mostrar/ocultar válidos
    var p2 = p.Mostrar(P(2));
    Assert.That(p2.Obtener(P(2)).Visible, Is.True);

    var p3 = p2.Ocultar(P(1));
    Assert.That(p3.Obtener(P(1)).Visible, Is.False);
    }

        [Test]
        public void ConOrden_hace_swap_si_el_nuevo_orden_esta_ocupado()
        {
            var p = PlantillaBasica();
            // Orden actual: P1=1, P2=2, P3=3
            var p2 = p.ConOrden(P(1), 3); // swap con P3

            Assert.That(p2.Columnas.Select(c => (c.Id.Numero, c.Orden)),
                Is.EqualTo(new[] { (2, (byte)2), (3, (byte)1), (1, (byte)3) }.OrderBy(x => x.Item2)
                    .Select(x => (x.Item1, x.Item2)))); // ver orden nuevo

            Assert.That(p2.Columnas[0].Id.Numero, Is.EqualTo(3)); // ahora P3 tiene orden 1
            Assert.That(p2.Columnas[1].Id.Numero, Is.EqualTo(2)); // P2 sigue 2
            Assert.That(p2.Columnas[2].Id.Numero, Is.EqualTo(1)); // P1 pasó a 3
        }

        [Test]
        public void Reemplazar_valida_invariantes_y_requiere_misma_Id()
        {
            var p = PlantillaBasica();
            var cfg2 = p.Obtener(P(2)).Renombrar(N("MAY+"));

            var p2 = p.Reemplazar(cfg2);
            Assert.That(p2.Obtener(P(2)).Nombre.Valor, Is.EqualTo("MAY+"));

            // Reemplazar Id inexistente
            var otro = Cfg(4, "Nuevo", orden:4); // Id P4 no existe en plantilla
            Assert.That(() => p.Reemplazar(otro), Throws.TypeOf<KeyNotFoundException>());

            // Violación de Base única (dos base)
            var cfg1dobleBase = p.Obtener(P(2)).MarcarComoBase();
            Assert.That(() => p.Reemplazar(cfg1dobleBase),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("Base"));
        }

        [Test]
        public void Agregar_y_Eliminar_respetan_reglas()
        {
            var p = PlantillaBasica();

            var p2 = p.Agregar(Cfg(4, "VIP", orden:4));
            Assert.That(p2.Count, Is.EqualTo(4));
            Assert.That(p2.Existe(P(4)), Is.True);

            // Agregar duplicado de Id
            Assert.That(() => p2.Agregar(Cfg(4, "X", orden:5)),
                Throws.TypeOf<InvalidOperationException>());

            // Agregar con orden ya ocupado (1..4 ya ocupados)
            Assert.That(() => p2.Agregar(Cfg(5, "DupOrden", orden:4)),
                Throws.TypeOf<InvalidOperationException>());

            // Eliminar no-base OK
            var p3 = p2.Eliminar(P(4));
            Assert.That(p3.Existe(P(4)), Is.False);

            // Eliminar base NO OK
            Assert.That(() => p3.Eliminar(P(1)), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Igualdad_y_hashcode_consideran_la_secuencia_ordenada()
        {
            var a = PlantillaBasica();
            var b = PlantillaBasica();
            var c = a.Renombrar(P(2), N("Otro"));

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            Assert.That(a.Equals(c), Is.False);
        }

        [Test]
        public void ToString_concatenado_legible()
        {
            var p = PlantillaBasica();
            var s = p.ToString();
            Assert.That(s, Does.Contain("P1"));
            Assert.That(s, Does.Contain("Base"));
            Assert.That(s, Does.Contain("|"));
        }
    }
}

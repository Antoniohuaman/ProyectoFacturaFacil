#nullable enable
using NUnit.Framework;
using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Tests.UnitTests.ValueObjects
{
    [TestFixture]
    public class PlantillaColumnasPrecioTests
    {
        // -------------------- Helpers --------------------
        private static IdentificadorColumnaPrecio P(byte n)
            => IdentificadorColumnaPrecio.DesdeNumero(n);

        private static NombreColumnaPrecio N(string texto)
            => NombreColumnaPrecio.Crear(texto);

        // Helper principal (Modo por defecto = Fijo)
        private static ConfiguracionColumnaPrecio C(
            byte num, string nombre, bool esBase, bool visible, byte orden)
            => ConfiguracionColumnaPrecio.Crear(
                id: P(num),
                nombre: N(nombre),
                modo: ModoValorizacionColumna.Fijo,
                esBase: esBase,
                visible: visible,
                orden: orden);

        // Overload por si algún test quiere modo PorVolumen explícito
        private static ConfiguracionColumnaPrecio CVol(
            byte num, string nombre, bool esBase, bool visible, byte orden)
            => ConfiguracionColumnaPrecio.Crear(
                id: P(num),
                nombre: N(nombre),
                modo: ModoValorizacionColumna.PorVolumen,
                esBase: esBase,
                visible: visible,
                orden: orden);

        // -------------------- Tests --------------------

        [Test]
        public void Expone_Base_IdColumnaBase_y_NumeroColumnaBase_en_creacion()
        {
            var p1 = C(1, "Precio público",  esBase: true,  visible: true,  orden: 1);
            var p2 = C(2, "Distribuidor",    esBase: false, visible: true,  orden: 2);

            // desordenado a propósito para validar normalización
            var plantilla = PlantillaColumnasPrecio.Crear(new[] { p2, p1 });

            Assert.That(plantilla.Base.Id.Numero, Is.EqualTo(1));
            Assert.That(plantilla.IdColumnaBase.Numero, Is.EqualTo(1));
            Assert.That(plantilla.NumeroColumnaBase, Is.EqualTo(1));
        }

        [Test]
        public void MarcarComoBase_actualiza_las_propiedades_de_Base_sin_mutar_la_instancia_anterior()
        {
            var p1 = C(1, "Precio público",  esBase: true,  visible: true, orden: 1);
            var p2 = C(2, "Distribuidor",    esBase: false, visible: true, orden: 2);

            var plantilla = PlantillaColumnasPrecio.Crear(new[] { p1, p2 });
            Assert.That(plantilla.NumeroColumnaBase, Is.EqualTo(1));

            var cambiada = plantilla.MarcarComoBase(P(2));
            Assert.That(cambiada.NumeroColumnaBase, Is.EqualTo(2));
            Assert.That(cambiada.IdColumnaBase.Numero, Is.EqualTo(2));

            // Inmutabilidad: la original permanece igual
            Assert.That(plantilla.NumeroColumnaBase, Is.EqualTo(1));
            Assert.That(plantilla.IdColumnaBase.Numero, Is.EqualTo(1));
        }

        [Test]
        public void Soporta_Base_distinta_de_P1_y_la_expone_correctamente()
        {
            var p1 = C(1, "Precio público",  esBase: false, visible: true, orden: 1);
            var p3 = C(3, "Mayorista",       esBase: true,  visible: true, orden: 3);

            var plantilla = PlantillaColumnasPrecio.Crear(new[] { p1, p3 });

            Assert.That(plantilla.NumeroColumnaBase, Is.EqualTo(3));
            Assert.That(plantilla.IdColumnaBase.Numero, Is.EqualTo(3));
            Assert.That(plantilla.Base.Id.Numero, Is.EqualTo(3));
        }

        [Test]
        public void Renombrar_no_afecta_la_Base()
        {
            var p1 = C(1, "Precio público",  esBase: true,  visible: true, orden: 1);
            var p2 = C(2, "Distribuidor",    esBase: false, visible: true, orden: 2);

            var plantilla = PlantillaColumnasPrecio.Crear(new[] { p1, p2 });
            var renombrada = plantilla.Renombrar(P(2), N("Canal mayorista"));

            Assert.That(renombrada.NumeroColumnaBase, Is.EqualTo(1));
            Assert.That(renombrada.IdColumnaBase.Numero, Is.EqualTo(1));
                Assert.That(renombrada.Obtener(P(2)).Nombre.Valor, Is.EqualTo("Canal mayorista"));
        }

        [Test]
        public void Reemplazar_una_columna_respeta_invariantes_y_conserva_la_Base()
        {
            var p1 = C(1, "Precio público", esBase: true, visible: true, orden: 1);
            var p2 = C(2, "Distribuidor",   esBase: false, visible: true, orden: 2);

            var plantilla = PlantillaColumnasPrecio.Crear(new[] { p1, p2 });

            var p2Nuevo = CVol(2, "Volumen", esBase: false, visible: true, orden: 2);
            var reemplazada = plantilla.Reemplazar(p2Nuevo);

            Assert.That(reemplazada.NumeroColumnaBase, Is.EqualTo(1));
            Assert.That(reemplazada.Obtener(P(2)).Modo, Is.EqualTo(ModoValorizacionColumna.PorVolumen));
        }

        [Test]
        public void Agregar_y_Eliminar_no_permiten_dejar_la_plantilla_sin_Base()
        {
            var p1 = C(1, "Público", esBase: true,  visible: true, orden: 1);
            var p2 = C(2, "VIP",     esBase: false, visible: true, orden: 2);

            var plantilla = PlantillaColumnasPrecio.Crear(new[] { p1, p2 });

            // Agregar una más (no base) y eliminarla => ok
            var p3 = C(3, "Promo", esBase: false, visible: true, orden: 3);
            var conP3 = plantilla.Agregar(p3);
            var sinP3 = conP3.Eliminar(P(3));
            Assert.That(sinP3.NumeroColumnaBase, Is.EqualTo(1));

            // Intentar eliminar la base => excepción
            Assert.That(() => plantilla.Eliminar(P(1)),
                Throws.InvalidOperationException.With.Message.Contains("Base"));
        }

        [Test]
        public void Ocultar_no_permite_quedar_sin_columnas_visibles_pero_no_toca_la_Base()
        {
            var p1 = C(1, "Público", esBase: true,  visible: true, orden: 1);
            var p2 = C(2, "VIP",     esBase: false, visible: true, orden: 2);

            var plantilla = PlantillaColumnasPrecio.Crear(new[] { p1, p2 });

            // Ocultar una que no es la última visible => ok
            var parcial = plantilla.Ocultar(P(2));
            Assert.That(parcial.NumeroColumnaBase, Is.EqualTo(1));

            // Si intentamos ocultar la última visible => excepción
            Assert.That(() => parcial.Ocultar(P(1)),
                Throws.InvalidOperationException.With.Message.Contains("última columna visible"));
        }

        [Test]
        public void ConOrden_hace_swap_si_el_orden_nuevo_esta_ocupado_y_no_afecta_la_Base()
        {
            var p1 = C(1, "Público", esBase: true,  visible: true, orden: 1);
            var p2 = C(2, "VIP",     esBase: false, visible: true, orden: 2);

            var plantilla = PlantillaColumnasPrecio.Crear(new[] { p1, p2 });
            var reordenada = plantilla.ConOrden(P(2), 1); // swap con P1

            // P1 queda en orden 2 y P2 en orden 1, pero la base sigue siendo P1
            Assert.That(reordenada.Obtener(P(1)).Orden, Is.EqualTo((byte)2));
            Assert.That(reordenada.Obtener(P(2)).Orden, Is.EqualTo((byte)1));
            Assert.That(reordenada.NumeroColumnaBase, Is.EqualTo(1));
            Assert.That(reordenada.IdColumnaBase.Numero, Is.EqualTo(1));
        }
    }
}

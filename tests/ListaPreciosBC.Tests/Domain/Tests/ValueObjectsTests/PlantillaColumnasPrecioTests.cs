#nullable enable
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ListaPreciosBC.Domain.ValueObjects;
using SharedKernel.Exceptions;

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

        private static ConfiguracionColumnaPrecio BaseCol(
            byte num, string nombre, bool visible, byte orden)
            => ConfiguracionColumnaPrecio.CrearBase(
                id: P(num),
                nombre: N(nombre),
                modo: ModoValorizacionColumna.Fijo,
                visible: visible,
                orden: orden);

        private static ConfiguracionColumnaPrecio ManualCol(
            byte num, string nombre, bool visible, byte orden)
            => ConfiguracionColumnaPrecio.CrearManual(
                id: CustomIdentificador(num),
                nombre: N(nombre),
                modo: ModoValorizacionColumna.Fijo,
                visible: visible,
                orden: orden);

        private static ConfiguracionColumnaPrecio ManualVol(
            byte num, string nombre, bool visible, byte orden)
            => ConfiguracionColumnaPrecio.CrearManual(
                id: CustomIdentificador(num),
                nombre: N(nombre),
                modo: ModoValorizacionColumna.PorVolumen,
                visible: visible,
                orden: orden);

        private static ConfiguracionColumnaPrecio GlobalDescuentoCol(
            byte num, string nombre, TipoReglaGlobalColumnaPrecio tipo, decimal valor, byte orden)
            => ConfiguracionColumnaPrecio.CrearGlobalDescuento(
                id: CustomIdentificador(num),
                nombre: N(nombre),
                modo: ModoValorizacionColumna.Fijo,
                regla: Regla(tipo, valor),
                visible: true,
                orden: orden);

        private static ReglaGlobalColumnaPrecio Regla(TipoReglaGlobalColumnaPrecio tipo, decimal valor)
            => ReglaGlobalColumnaPrecio.Crear(tipo, valor);

        private static IdentificadorColumnaPrecio CustomIdentificador(byte numero)
        {
            if (IdentificadorColumnaPrecio.TryDesdeNumero(numero, out var id))
            {
                return id!;
            }

            var ctor = typeof(IdentificadorColumnaPrecio)
                .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(string), typeof(byte) }, null)
                ?? throw new InvalidOperationException("No se pudo acceder al constructor interno de IdentificadorColumnaPrecio.");

            return (IdentificadorColumnaPrecio)ctor.Invoke(new object[] { $"P{numero}", numero });
        }

        // -------------------- Tests --------------------

        [Test]
        public void Expone_Base_IdColumnaBase_y_NumeroColumnaBase_en_creacion()
        {
            var p1 = BaseCol(1, "Precio público", visible: true, orden: 1);
            var p2 = ManualCol(2, "Distribuidor", visible: true, orden: 2);

            // desordenado a propósito para validar normalización
            var plantilla = PlantillaColumnasPrecio.Crear(new[] { p2, p1 });

            Assert.That(plantilla.Base.Id.Numero, Is.EqualTo(1));
            Assert.That(plantilla.IdColumnaBase.Numero, Is.EqualTo(1));
            Assert.That(plantilla.NumeroColumnaBase, Is.EqualTo(1));
        }

        [Test]
        public void MarcarComoBase_actualiza_las_propiedades_de_Base_sin_mutar_la_instancia_anterior()
        {
            var p1 = BaseCol(1, "Precio público", visible: true, orden: 1);
            var p2 = ManualCol(2, "Distribuidor", visible: true, orden: 2);

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
            var p1 = ManualCol(1, "Precio público", visible: true, orden: 1);
            var p3 = ManualCol(3, "Mayorista", visible: true, orden: 3).MarcarComoBase();

            var plantilla = PlantillaColumnasPrecio.Crear(new[] { p1, p3 });

            Assert.That(plantilla.NumeroColumnaBase, Is.EqualTo(3));
            Assert.That(plantilla.IdColumnaBase.Numero, Is.EqualTo(3));
            Assert.That(plantilla.Base.Id.Numero, Is.EqualTo(3));
        }

        [Test]
        public void Renombrar_no_afecta_la_Base()
        {
            var p1 = BaseCol(1, "Precio público", visible: true, orden: 1);
            var p2 = ManualCol(2, "Distribuidor", visible: true, orden: 2);

            var plantilla = PlantillaColumnasPrecio.Crear(new[] { p1, p2 });
            var renombrada = plantilla.Renombrar(P(2), N("Canal mayorista"));

            Assert.That(renombrada.NumeroColumnaBase, Is.EqualTo(1));
            Assert.That(renombrada.IdColumnaBase.Numero, Is.EqualTo(1));
                Assert.That(renombrada.Obtener(P(2)).Nombre.Valor, Is.EqualTo("Canal mayorista"));
        }

        [Test]
        public void Reemplazar_una_columna_respeta_invariantes_y_conserva_la_Base()
        {
            var p1 = BaseCol(1, "Precio público", visible: true, orden: 1);
            var p2 = ManualCol(2, "Distribuidor", visible: true, orden: 2);

            var plantilla = PlantillaColumnasPrecio.Crear(new[] { p1, p2 });

            var p2Nuevo = ManualVol(2, "Volumen", visible: true, orden: 2);
            var reemplazada = plantilla.Reemplazar(p2Nuevo);

            Assert.That(reemplazada.NumeroColumnaBase, Is.EqualTo(1));
            Assert.That(reemplazada.Obtener(P(2)).Modo, Is.EqualTo(ModoValorizacionColumna.PorVolumen));
        }

        [Test]
        public void Agregar_y_Eliminar_no_permiten_dejar_la_plantilla_sin_Base()
        {
            var p1 = BaseCol(1, "Público", visible: true, orden: 1);
            var p2 = ManualCol(2, "VIP", visible: true, orden: 2);

            var plantilla = PlantillaColumnasPrecio.Crear(new[] { p1, p2 });

            // Agregar una más (no base) y eliminarla => ok
            var p3 = ManualCol(3, "Promo", visible: true, orden: 3);
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
            var p1 = BaseCol(1, "Público", visible: true, orden: 1);
            var p2 = ManualCol(2, "VIP", visible: true, orden: 2);

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
            var p1 = BaseCol(1, "Público", visible: true, orden: 1);
            var p2 = ManualCol(2, "VIP", visible: true, orden: 2);

            var plantilla = PlantillaColumnasPrecio.Crear(new[] { p1, p2 });
            var reordenada = plantilla.ConOrden(P(2), 1); // swap con P1

            // P1 queda en orden 2 y P2 en orden 1, pero la base sigue siendo P1
            Assert.That(reordenada.Obtener(P(1)).Orden, Is.EqualTo((byte)2));
            Assert.That(reordenada.Obtener(P(2)).Orden, Is.EqualTo((byte)1));
            Assert.That(reordenada.NumeroColumnaBase, Is.EqualTo(1));
            Assert.That(reordenada.IdColumnaBase.Numero, Is.EqualTo(1));
        }

        [Test]
        public void No_permite_mas_de_diez_columnas_manuales()
        {
            var baseCol = BaseCol(1, "Base", visible: true, orden: 1);
            var manuales = Enumerable.Range(2, 10)
                .Select(i => ManualCol((byte)i, $"Manual {i}", visible: true, orden: (byte)i))
                .ToList();

            var plantilla = PlantillaColumnasPrecio.Crear(manuales.Prepend(baseCol));

            var onceava = ManualCol(25, "Manual extra", visible: true, orden: 25);

            Assert.That(() => plantilla.Agregar(onceava),
                Throws.TypeOf<BusinessRuleException>().With.Message.Contains("manuales"));
        }

        [Test]
        public void No_permite_mas_de_una_columna_base()
        {
            var base1 = BaseCol(1, "Base 1", visible: true, orden: 1);
            var base2 = BaseCol(2, "Base 2", visible: true, orden: 2);

            Assert.That(
                () => PlantillaColumnasPrecio.Crear(new[] { base1, base2 }),
                Throws.InvalidOperationException.With.Message.Contains("Base"));
        }

        [Test]
        public void Columnas_globales_y_manuales_se_exponen_por_separado()
        {
            var baseCol = BaseCol(1, "Base", visible: true, orden: 1);
            var manual = ManualCol(2, "Manual", visible: true, orden: 2);
            var global = GlobalDescuentoCol(3, "Global", TipoReglaGlobalColumnaPrecio.MontoFijo, 10m, orden: 3);

            var plantilla = PlantillaColumnasPrecio.Crear(new[] { baseCol, manual, global });

            Assert.That(plantilla.ColumnasGlobales.Count(), Is.EqualTo(1));
            Assert.That(plantilla.ColumnasManuales.Count(), Is.EqualTo(1));
            Assert.That(plantilla.ColumnasGlobales.First().Tipo, Is.EqualTo(TipoColumnaPrecio.GlobalDescuento));
            Assert.That(plantilla.ColumnasManuales.First().Tipo, Is.EqualTo(TipoColumnaPrecio.Manual));
        }
    }
}

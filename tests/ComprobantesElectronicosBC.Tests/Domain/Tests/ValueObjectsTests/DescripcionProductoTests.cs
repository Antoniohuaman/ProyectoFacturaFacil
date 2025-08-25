using System;
using System.Linq;
using NUnit.Framework;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.UnitTests.ValueObjects
{
    [TestFixture]
    public class DescripcionProductoTests
    {
        // ----------------------
        // Create / FromCatalogName
        // ----------------------

        [Test]
        public void Create_Normaliza_Trim_ColapsaEspacios_Y_LimitaLongitudes()
        {
            // Nombre con tabs/espacios + control chars; Detalle con múltiples líneas y espacios
            var nombreRaw  = "  \t  LADRILLO \u0001  KING   KONG\t  ";
            var detalleRaw = "  Color rojo  \n \t Dimensiones: 18x13x9  \n  \u0002 \n  ";

            var vo = DescripcionProducto.Create(nombreRaw, detalleRaw);

            Assert.Multiple(() =>
            {
                Assert.That(vo.Nombre, Is.EqualTo("LADRILLO KING KONG")); // tabs/espacios colapsados, control chars fuera
                Assert.That(vo.Detalle, Is.EqualTo("Color rojo\nDimensiones: 18x13x9")); // líneas limpias y sin vacías
            });
        }

        [Test]
        public void Create_Rechaza_Nombre_Vacio()
        {
            Assert.Throws<ArgumentException>(() => DescripcionProducto.Create("  "));
            Assert.Throws<ArgumentException>(() => DescripcionProducto.Create(null));
        }

        [Test]
        public void Create_Trunca_Nombre_y_Detalle_Segun_Maximos()
        {
            var nombreLargo  = new string('N', DescripcionProducto.MaxNombre + 50);
            var detalleLargo = new string('D', DescripcionProducto.MaxDetalle + 200);

            var vo = DescripcionProducto.Create(nombreLargo, detalleLargo);

            Assert.Multiple(() =>
            {
                Assert.That(vo.Nombre.Length, Is.EqualTo(DescripcionProducto.MaxNombre));
                Assert.That(vo.Detalle!.Length, Is.EqualTo(DescripcionProducto.MaxDetalle));
            });
        }

        [Test]
        public void FromCatalogName_Equivale_A_Create()
        {
            var a = DescripcionProducto.Create("Producto A", "Detalle A");
            var b = DescripcionProducto.FromCatalogName("Producto A", "Detalle A");
            Assert.That(a, Is.EqualTo(b));
        }

        // ----------------------
        // Mutadores inmutables (WithAppendedDetail)
        // ----------------------

        [Test]
        public void WithAppendedDetail_Agrega_Linea_Nueva_Respetando_MaxDetalle()
        {
            var baseVo = DescripcionProducto.Create("Prod", "Linea 1");

            var vo = baseVo.WithAppendedDetail("  Linea 2  ").WithAppendedDetail("Linea 3");

            Assert.That(vo.Detalle, Is.EqualTo("Linea 1\nLinea 2\nLinea 3"));

            // Prueba de truncado
            var baseGrande = new string('X', DescripcionProducto.MaxDetalle - 1);
            var vo2 = DescripcionProducto.Create("Prod", baseGrande).WithAppendedDetail("YYYYY");

            Assert.That(vo2.Detalle!.Length, Is.EqualTo(DescripcionProducto.MaxDetalle)); // truncado
        }

        [Test]
        public void WithAppendedDetail_Sobre_Detalle_Null_Crea_Detalle()
        {
            var vo = DescripcionProducto.Create("Prod", null).WithAppendedDetail("Nuevo detalle");
            Assert.That(vo.Detalle, Is.EqualTo("Nuevo detalle"));
        }

        // ----------------------
        // Salidas para PDF/UBL
        // ----------------------

        [Test]
        public void ToPdfSingleLine_Concatena_Nombre_Y_Detalle_En_Una_Sola_Linea()
        {
            var vo = DescripcionProducto.Create("Martillo", "Mango de madera\t y cabeza de acero");
            var line = vo.ToPdfSingleLine();

            // Debe colapsar tabs y espacios en detalle y unir con " — "
            Assert.That(line, Does.Contain("Martillo — Mango de madera y cabeza de acero"));
            Assert.That(line, Does.Not.Contain("\n"));
        }

        [Test]
        public void ToPdfMultiLine_Primera_Linea_Nombre_Luego_Detalle()
        {
            var vo = DescripcionProducto.Create("Taladro", "500W\nIncluye 2 brocas");
            var multi = vo.ToPdfMultiLine();

            var lines = multi.Split('\n');
            Assert.Multiple(() =>
            {
                Assert.That(lines[0], Is.EqualTo("Taladro"));
                Assert.That(lines[1], Is.EqualTo("500W"));
                Assert.That(lines[2], Is.EqualTo("Incluye 2 brocas"));
            });
        }

        [Test]
        public void ToUblDescriptions_Primera_Elemento_Nombre_Resto_Lineas_Detalle()
        {
            var vo = DescripcionProducto.Create("Cemento Portland", "Saco 42.5kg \n Uso general \n\n Secado rápido");
            var ubl = vo.ToUblDescriptions();

            Assert.Multiple(() =>
            {
                Assert.That(ubl[0], Is.EqualTo("Cemento Portland")); // nombre
                Assert.That(ubl[1], Is.EqualTo("Saco 42.5kg"));
                Assert.That(ubl[2], Is.EqualTo("Uso general"));
                Assert.That(ubl[3], Is.EqualTo("Secado rápido"));
                Assert.That(ubl.Count, Is.EqualTo(4));
            });
        }

        [Test]
        public void ToUblDescriptions_Solo_Nombre_Si_No_Hay_Detalle()
        {
            var vo = DescripcionProducto.Create("Cable UTP Cat6");
            var ubl = vo.ToUblDescriptions();

            Assert.That(ubl.Count, Is.EqualTo(1));
            Assert.That(ubl[0], Is.EqualTo("Cable UTP Cat6"));
        }

        // ----------------------
        // Normalización: control chars y tabs
        // ----------------------

        [Test]
        public void Normaliza_Elimina_ControlChars_Pero_Conserva_NL_Y_Tab_Colapsando_Espacios()
        {
            var nombre = " \u0000  Prod\t\t  \u0007 ABC  ";
            var detalle = "Linea\u0001 1 \n \u0003 Linea\t\t2";

            var vo = DescripcionProducto.Create(nombre, detalle);

            Assert.Multiple(() =>
            {
                // Tabs/espacios colapsados y control chars fuera
                Assert.That(vo.Nombre, Is.EqualTo("Prod ABC"));
                Assert.That(vo.Detalle, Is.EqualTo("Linea 1\nLinea 2"));
            });
        }

        // ----------------------
        // Igualdad por valor (record)
        // ----------------------

        [Test]
        public void Equality_Dos_Instancias_Mismo_Contenido_Son_Iguales()
        {
            var a = DescripcionProducto.Create("Mouse", "Óptico 1200dpi");
            var b = DescripcionProducto.Create("Mouse", "Óptico 1200dpi");
            var c = DescripcionProducto.Create("Mouse", "Óptico 1600dpi");

            Assert.Multiple(() =>
            {
                Assert.That(a, Is.EqualTo(b));
                Assert.That(a, Is.Not.EqualTo(c));
            });
        }
    }
}
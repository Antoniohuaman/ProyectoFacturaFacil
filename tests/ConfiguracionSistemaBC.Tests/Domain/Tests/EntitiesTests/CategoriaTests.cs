using System;
using NUnit.Framework;
using ConfiguracionSistemaBC.Domain.Entities;
using NUnit.Framework;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.Domain
{
    [TestFixture]
    public class CategoriaTests
    {
        private static EmpresaId Emp() => EmpresaId.From(Guid.NewGuid().ToString());
        private static string HexVerde() => "#27AE60";

        [Test]
        public void Crear_Valido_InicializaCorrecto()
        {
            var cat = Categoria.Crear(Emp(), "Bebidas", "Gaseosas y jugos", HexVerde());

            Assert.That(cat.Id, Is.Not.EqualTo(default(CategoriaId)));
            Assert.That(cat.EmpresaId, Is.Not.EqualTo(default(EmpresaId)));
            Assert.That(cat.Nombre, Is.EqualTo("BEBIDAS"));
            Assert.That(cat.Descripcion, Is.EqualTo("Gaseosas y jugos"));
            Assert.That(cat.ColorHex, Is.EqualTo(HexVerde()));
            Assert.That(cat.Estado, Is.EqualTo(EstadoCategoria.Habilitado));
            Assert.That(cat.FechaRegistroUtc, Is.Not.EqualTo(default(DateTime)));
            Assert.That(cat.FechaUltimaModificacionUtc, Is.Null);
        }

        [Test]
        public void Crear_NombreVacio_Lanza()
        {
            Assert.That(() => Categoria.Crear(Emp(), "  "), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Crear_NombreMuyLargo_Lanza()
        {
            var largo101 = new string('A', 101);
            Assert.That(() => Categoria.Crear(Emp(), largo101), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Crear_ColorHexInvalido_Lanza()
        {
            Assert.That(() => Categoria.Crear(Emp(), "Bebidas", colorHex: "verde"), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Renombrar_CambiaNombreYMarcaModificacion()
        {
            var cat = Categoria.Crear(Emp(), "Bebidas");
            cat.Renombrar("Lácteos");

            Assert.That(cat.Nombre, Is.EqualTo("LÁCTEOS"));
            Assert.That(cat.FechaUltimaModificacionUtc, Is.Not.Null);
        }

        [Test]
        public void Renombrar_MismoNombre_Lanza()
        {
            var cat = Categoria.Crear(Emp(), "Bebidas");
            Assert.That(() => cat.Renombrar("  bebidas "), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void CambiarDescripcion_ActualizaYMarcaModificacion()
        {
            var cat = Categoria.Crear(Emp(), "Bebidas", "A");
            cat.CambiarDescripcion("B");

            Assert.That(cat.Descripcion, Is.EqualTo("B"));
            Assert.That(cat.FechaUltimaModificacionUtc, Is.Not.Null);
        }

        [Test]
        public void CambiarDescripcion_Misma_Lanza()
        {
            var cat = Categoria.Crear(Emp(), "Bebidas", "Texto");
            Assert.That(() => cat.CambiarDescripcion("  Texto "), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void CambiarColor_ActualizaYMarcaModificacion()
        {
            var cat = Categoria.Crear(Emp(), "Bebidas", colorHex: HexVerde());
            cat.CambiarColor("#2D9CDB");

            Assert.That(cat.ColorHex, Is.EqualTo("#2D9CDB"));
            Assert.That(cat.FechaUltimaModificacionUtc, Is.Not.Null);
        }

        [Test]
        public void CambiarColor_Mismo_Lanza()
        {
            var cat = Categoria.Crear(Emp(), "Bebidas", colorHex: HexVerde());
            Assert.That(() => cat.CambiarColor(HexVerde()), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Habilitar_DesdeDeshabilitado_CambiaEstado()
        {
            var cat = Categoria.Crear(Emp(), "Bebidas");
            cat.Deshabilitar();
            cat.Habilitar();

            Assert.That(cat.Estado, Is.EqualTo(EstadoCategoria.Habilitado));
            Assert.That(cat.FechaUltimaModificacionUtc, Is.Not.Null);
        }

        [Test]
        public void Deshabilitar_CambiaEstado()
        {
            var cat = Categoria.Crear(Emp(), "Bebidas");
            cat.Deshabilitar();

            Assert.That(cat.Estado, Is.EqualTo(EstadoCategoria.Deshabilitado));
            Assert.That(cat.FechaUltimaModificacionUtc, Is.Not.Null);
        }

        [Test]
        public void Habilitar_YaHabilitado_Lanza()
        {
            var cat = Categoria.Crear(Emp(), "Bebidas");
            Assert.That(() => cat.Habilitar(), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Deshabilitar_YaDeshabilitado_Lanza()
        {
            var cat = Categoria.Crear(Emp(), "Bebidas");
            cat.Deshabilitar();
            Assert.That(() => cat.Deshabilitar(), Throws.TypeOf<InvalidOperationException>());
        }
    }
}

using System;
using NUnit.Framework;
using CatalogoArticulosBC.Domain.Entities;

namespace CatalogoArticulosBC.Tests.Domain.Entities
{
    [TestFixture]
    public class MultimediaProductoTests
    {
        // ----------------------------
        // Construcción válida
        // ----------------------------
        [Test]
        public void Constructor_Valido_AsignaPropiedades_Y_FechaCargaUtcDentroDeRango()
        {
            // arrange
            var id = Guid.NewGuid();
            var before = DateTime.UtcNow;

            // act
            var m = new MultimediaProducto(
                multimediaId: id,
                tipoMime: "image/jpeg",
                tipoAdjunto: "ImagenPrincipal",
                nombreArchivo: "foto.jpg",
                ruta: "/media/foto.jpg",
                comentario: "Primera foto",
                tamano: 1024L
            );

            var after = DateTime.UtcNow;

            // assert
            Assert.Multiple(() =>
            {
                Assert.That(m.MultimediaId, Is.EqualTo(id));
                Assert.That(m.TipoMime, Is.EqualTo("image/jpeg"));
                Assert.That(m.TipoAdjunto, Is.EqualTo("ImagenPrincipal"));
                Assert.That(m.NombreArchivo, Is.EqualTo("foto.jpg"));
                Assert.That(m.Ruta, Is.EqualTo("/media/foto.jpg"));
                Assert.That(m.Comentario, Is.EqualTo("Primera foto"));
                Assert.That(m.Tamano, Is.EqualTo(1024L));

                Assert.That(m.FechaCarga.Kind, Is.EqualTo(DateTimeKind.Utc), "FechaCarga debe ser UTC");
                Assert.That(m.FechaCarga, Is.InRange(before, after), "FechaCarga debe quedar entre antes y después de construir");
            });
        }

        // ----------------------------
        // Trimming de cadenas
        // ----------------------------
        [Test]
        public void Constructor_TrimeaCadenas_Y_NormalizaComentarioNuloOBlanco()
        {
            var id = Guid.NewGuid();

            var m1 = new MultimediaProducto(
                multimediaId: id,
                tipoMime: "  image/png  ",
                tipoAdjunto: "  ManualPDF  ",
                nombreArchivo: "  doc.pdf  ",
                ruta: "  /docs/archivo.pdf  ",
                comentario: "  nota  ",
                tamano: 1L
            );

            Assert.Multiple(() =>
            {
                Assert.That(m1.TipoMime, Is.EqualTo("image/png"));
                Assert.That(m1.TipoAdjunto, Is.EqualTo("ManualPDF"));
                Assert.That(m1.NombreArchivo, Is.EqualTo("doc.pdf"));
                Assert.That(m1.Ruta, Is.EqualTo("/docs/archivo.pdf"));
                Assert.That(m1.Comentario, Is.EqualTo("nota"));
            });

            // comentario null -> string.Empty
            var m2 = new MultimediaProducto(
                multimediaId: Guid.NewGuid(),
                tipoMime: "image/png",
                tipoAdjunto: "ManualPDF",
                nombreArchivo: "doc.pdf",
                ruta: "/docs/archivo.pdf",
                comentario: null!,
                tamano: 10L
            );
            Assert.That(m2.Comentario, Is.EqualTo(string.Empty));

            // comentario espacios -> string.Empty tras Trim
            var m3 = new MultimediaProducto(
                multimediaId: Guid.NewGuid(),
                tipoMime: "image/png",
                tipoAdjunto: "ManualPDF",
                nombreArchivo: "doc.pdf",
                ruta: "/docs/archivo.pdf",
                comentario: "   ",
                tamano: 10L
            );
            Assert.That(m3.Comentario, Is.EqualTo(string.Empty));
        }

        // ----------------------------
        // Validaciones: ID
        // ----------------------------
        [Test]
        public void Constructor_IdVacio_LanzaArgumentException()
        {
            Assert.That(() =>
                new MultimediaProducto(
                    multimediaId: Guid.Empty,
                    tipoMime: "image/png",
                    tipoAdjunto: "Imagen",
                    nombreArchivo: "f.png",
                    ruta: "/x/f.png",
                    comentario: "",
                    tamano: 1L),
                Throws.ArgumentException
                      .With.Property("ParamName").EqualTo("multimediaId"));
        }

        // ----------------------------
        // Validaciones: strings requeridos
        // ----------------------------
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_TipoMimeInvalido_LanzaArgumentException(string? invalido)
        {
            Assert.That(() =>
                new MultimediaProducto(Guid.NewGuid(), invalido!, "Imagen", "f.png", "/x/f.png", "", 1L),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("tipoMime"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_TipoAdjuntoInvalido_LanzaArgumentException(string? invalido)
        {
            Assert.That(() =>
                new MultimediaProducto(Guid.NewGuid(), "image/png", invalido!, "f.png", "/x/f.png", "", 1L),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("tipoAdjunto"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_NombreArchivoInvalido_LanzaArgumentException(string? invalido)
        {
            Assert.That(() =>
                new MultimediaProducto(Guid.NewGuid(), "image/png", "Imagen", invalido!, "/x/f.png", "", 1L),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("nombreArchivo"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_RutaInvalida_LanzaArgumentException(string? invalido)
        {
            Assert.That(() =>
                new MultimediaProducto(Guid.NewGuid(), "image/png", "Imagen", "f.png", invalido!, "", 1L),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("ruta"));
        }

        // ----------------------------
        // Validaciones: tamaño
        // ----------------------------
        [TestCase(0L)]
        [TestCase(-1L)]
        public void Constructor_TamanoNoPositivo_LanzaArgumentException(long tamano)
        {
            Assert.That(() =>
                new MultimediaProducto(Guid.NewGuid(), "image/png", "Imagen", "f.png", "/x/f.png", "", tamano),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("tamano"));
        }
    }
}

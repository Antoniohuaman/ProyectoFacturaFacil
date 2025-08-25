using System;
using NUnit.Framework;
using ConfiguracionSistemaBC.Domain.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.UnitTests.ValueObjects
{
    [TestFixture]
    public class LogoImagenTests
    {
        [Test]
        public void FromUpload_Valido_Png()
        {
            var logo = LogoImagen.FromUpload(
                fileName: "Mi Logo 2025(versión).PNG",
                contentType: "image/png",
                bytesLength: 120_000,
                anchoPx: 600,
                altoPx: 200);

            Assert.That(logo.ContentType, Is.EqualTo("image/png"));
            Assert.That(logo.Extension, Is.EqualTo(".png"));
            Assert.That(logo.AnchoPx, Is.EqualTo(600));
            Assert.That(logo.AltoPx, Is.EqualTo(200));
            Assert.That(logo.AspectRatio, Is.EqualTo(3.0).Within(0.0001));

            // Nombre sanitizado: sin espacios ni paréntesis, conserva letras/dígitos/_-. y termina en .png
            Assert.That(logo.NombreArchivo, Does.EndWith(".png"));
            Assert.That(logo.NombreArchivo, Does.Not.Contain(" "));
            Assert.That(logo.NombreArchivo, Does.Not.Contain("("));
            Assert.That(logo.NombreArchivo, Does.Not.Contain(")"));
        }

        [Test]
        public void FromUpload_Valido_Jpeg_NormalizaExtension()
        {
            var logo = LogoImagen.FromUpload(
                fileName: "marca.JPEG",
                contentType: "image/JPEG", // mayúsculas/mixto aceptado
                bytesLength: 50_000,
                anchoPx: 400,
                altoPx: 200);

            Assert.That(logo.ContentType, Is.EqualTo("image/JPEG"));
            Assert.That(logo.Extension, Is.EqualTo(".jpg")); // normalizado
            Assert.That(logo.NombreArchivo, Does.EndWith(".jpg"));
        }

        [Test]
        public void FromUpload_ExtensionNoPermitida_Lanza()
        {
            Assert.That(() => LogoImagen.FromUpload(
                fileName: "logo.svg",
                contentType: "image/svg+xml",
                bytesLength: 10_000,
                anchoPx: 300,
                altoPx: 150),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void FromUpload_MimeNoPermitido_Lanza()
        {
            Assert.That(() => LogoImagen.FromUpload(
                fileName: "logo.png",
                contentType: "application/octet-stream",
                bytesLength: 10_000,
                anchoPx: 300,
                altoPx: 150),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void FromUpload_TamanoBytes_FueraDeRango_Lanza()
        {
            // 0 bytes
            Assert.That(() => LogoImagen.FromUpload(
                fileName: "logo.png",
                contentType: "image/png",
                bytesLength: 0,
                anchoPx: 300,
                altoPx: 150),
                Throws.TypeOf<ArgumentOutOfRangeException>());

            // Mayor que MaxBytes
            Assert.That(() => LogoImagen.FromUpload(
                fileName: "logo.jpg",
                contentType: "image/jpeg",
                bytesLength: LogoImagen.MaxBytes + 1,
                anchoPx: 300,
                altoPx: 150),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void FromUpload_Dimensiones_FueraDeRango_Lanza()
        {
            // Demasiado pequeño
            Assert.That(() => LogoImagen.FromUpload(
                fileName: "logo.png",
                contentType: "image/png",
                bytesLength: 20_000,
                anchoPx: 40,   // < MinAnchoPx
                altoPx: 20),   // < MinAltoPx
                Throws.TypeOf<ArgumentOutOfRangeException>());

            // Demasiado grande
            Assert.That(() => LogoImagen.FromUpload(
                fileName: "logo.png",
                contentType: "image/png",
                bytesLength: 20_000,
                anchoPx: LogoImagen.MaxAnchoPx + 1,
                altoPx: LogoImagen.MinAltoPx),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void FromUpload_AspectRatio_Extremo_Lanza()
        {
            // Muy ancho (ratio > MaxAspecto)
            Assert.That(() => LogoImagen.FromUpload(
                fileName: "wider.png",
                contentType: "image/png",
                bytesLength: 30_000,
                anchoPx: 1200,
                altoPx: 100),
                Throws.TypeOf<ArgumentOutOfRangeException>());

            // Muy alto (ratio < MinAspecto)
            Assert.That(() => LogoImagen.FromUpload(
                fileName: "tall.png",
                contentType: "image/png",
                bytesLength: 30_000,
                anchoPx: 100,
                altoPx: 1200),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TryFromUpload_ComportamientoEsperado()
        {
            // OK
            var ok = LogoImagen.TryFromUpload("ok.png", "image/png", 10_000, 300, 120, out var logo);
            Assert.That(ok, Is.True);
            Assert.That(logo, Is.Not.Null);

            // Nulls => false
            Assert.That(LogoImagen.TryFromUpload(null, "image/png", 10_000, 300, 120, out _), Is.False);
            Assert.That(LogoImagen.TryFromUpload("x.png", null, 10_000, 300, 120, out _), Is.False);

            // Invalidez (ex: ancho muy grande) => false
            Assert.That(LogoImagen.TryFromUpload("x.png", "image/png", 10_000, LogoImagen.MaxAnchoPx + 5, 120, out _), Is.False);
        }

        [Test]
        public void SanitizarNombre_TruncaYLimpia()
        {
            var largo = new string('A', 100) + ".png";
            var logo = LogoImagen.FromUpload(
                fileName: largo,
                contentType: "image/png",
                bytesLength: 12_345,
                anchoPx: 300,
                altoPx: 150);

            // Debe truncar a 80 chars + ".png"
            Assert.That(logo.NombreArchivo, Does.EndWith(".png"));
            Assert.That(logo.NombreArchivo.Length, Is.EqualTo(80 + 4));

            // Sin caracteres peligrosos
            Assert.That(logo.NombreArchivo, Does.Not.Contain(" "));
            Assert.That(logo.NombreArchivo, Does.Not.Contain("/"));
            Assert.That(logo.NombreArchivo, Does.Not.Contain("\\"));
        }

        [Test]
        public void FitIn_NoEscalaHaciaArriba_Y_EscalaProporcional()
        {
            // Logo más grande que el bound 200x200
            var logo = LogoImagen.FromUpload("big.png", "image/png", 50_000, 800, 400);
            var (w1, h1) = logo.FitIn(200, 200);
            Assert.That(w1, Is.EqualTo(200));
            Assert.That(h1, Is.EqualTo(100)); // escala 0.25

            // Logo ya más pequeño que el bound: no debe escalar
            var logoSmall = LogoImagen.FromUpload("small.png", "image/png", 10_000, 100, 50);
            var (w2, h2) = logoSmall.FitIn(220, 80);
            Assert.That(w2, Is.EqualTo(100));
            Assert.That(h2, Is.EqualTo(50));
        }

        [Test]
        public void FitCabeceraA4_UsaBound_220x80()
        {
            var logo = LogoImagen.FromUpload("head.png", "image/png", 40_000, 600, 120);
            var (w1, h1) = logo.FitCabeceraA4();
            var (w2, h2) = logo.FitIn(220, 80);

            Assert.That(w1, Is.EqualTo(w2));
            Assert.That(h1, Is.EqualTo(h2));
            Assert.That((w1, h1), Is.EqualTo((220, 44))); // 600x120 -> escala 220/600 = 0.3666...
        }

        [Test]
        public void ToString_E_IgualdadYHash()
        {
            var a = LogoImagen.FromUpload("x.jpg", "image/jpeg", 30_000, 400, 200);
            var b = LogoImagen.FromUpload("x.jpg", "image/jpeg", 30_000, 400, 200);
            var c = LogoImagen.FromUpload("y.jpg", "image/jpeg", 30_000, 401, 200);

            // ToString informativo
            var s = a.ToString();
            Assert.That(s, Does.Contain("x.jpg"));
            Assert.That(s, Does.Contain("400x200"));
            Assert.That(s, Does.Contain("image/jpeg"));

            // Igualdad por metadatos
            Assert.That(a.Equals(b), Is.True);
            Assert.That(a == b, Is.True);
            Assert.That(a != b, Is.False);
            Assert.That(a.Equals(c), Is.False);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }
    }
}
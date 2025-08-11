using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace ConfiguracionSistemaBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object para el logotipo de la empresa.
    /// - Formatos: PNG o JPEG.
    /// - Guarda metadatos del archivo subido (nombre, content-type, tamaño, dimensiones).
    /// - Valida límites prudentes para un logo que va a PDF/impresión.
    /// - Provee helper para calcular tamaño de render (fit proporcional).
    ///
    /// NOTA: La carga/lectura real de bytes y la obtención de (width,height) se hace
    /// en Adapters/Infra. Este VO asume que ya conoces las dimensiones.
    /// </summary>
    [DebuggerDisplay("{NombreArchivo} {AnchoPx}x{AltoPx} ({ContentType}, {BytesLength} bytes)")]
    public sealed class LogoImagen
    {
        // ------------------- Límites y formatos permitidos -------------------
        /// <summary>Tamaño máximo del archivo (1 MiB).</summary>
        public const long MaxBytes = 1 * 1024 * 1024;

        /// <summary>Dimensiones mínimas recomendadas (evitar logos borrosos).</summary>
        public const int MinAnchoPx = 64;
        public const int MinAltoPx  = 32;

        /// <summary>Dimensiones máximas razonables (no tiene sentido más grande para PDF).</summary>
        public const int MaxAnchoPx = 1600;
        public const int MaxAltoPx  = 1200;

        /// <summary>Aspect ratio permitido (ancho/alto) para evitar casos extremos.</summary>
        public const double MinAspecto = 0.15; // muy alto
        public const double MaxAspecto = 6.0;  // muy ancho

        private static readonly HashSet<string> _extPermitidas = new(StringComparer.OrdinalIgnoreCase)
        { ".png", ".jpg", ".jpeg" };

        private static readonly HashSet<string> _mimePermitidos = new(StringComparer.OrdinalIgnoreCase)
        { "image/png", "image/jpeg" };

        // ------------------- Estado inmutable -------------------
        /// <summary>Nombre de archivo seguro (sanitizado).</summary>
        public string NombreArchivo { get; }

        /// <summary>MIME type (image/png o image/jpeg).</summary>
        public string ContentType { get; }

        /// <summary>Tamaño del archivo en bytes.</summary>
        public long BytesLength { get; }

        /// <summary>Dimensiones en píxeles.</summary>
        public int AnchoPx { get; }
        public int AltoPx  { get; }

        /// <summary>Relación ancho/alto.</summary>
        public double AspectRatio => (double)AnchoPx / AltoPx;

        /// <summary>Extensión (normalizada a .png o .jpg).</summary>
        public string Extension { get; }

        private LogoImagen(string nombreArchivo, string contentType, long bytesLength, int anchoPx, int altoPx, string extension)
        {
            NombreArchivo = nombreArchivo;
            ContentType   = contentType;
            BytesLength   = bytesLength;
            AnchoPx       = anchoPx;
            AltoPx        = altoPx;
            Extension     = extension;
        }

        // ------------------- Fábricas -------------------

        /// <summary>
        /// Crea un <see cref="LogoImagen"/> desde los metadatos provistos por la capa de subida.
        /// </summary>
        /// <param name="fileName">Nombre original del archivo.</param>
        /// <param name="contentType">Content-Type detectado (image/png o image/jpeg).</param>
        /// <param name="bytesLength">Tamaño del archivo.</param>
        /// <param name="anchoPx">Ancho en píxeles.</param>
        /// <param name="altoPx">Alto en píxeles.</param>
        public static LogoImagen FromUpload(string fileName, string contentType, long bytesLength, int anchoPx, int altoPx)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(nameof(fileName));

            if (string.IsNullOrWhiteSpace(contentType))
                throw new ArgumentNullException(nameof(contentType));

            if (bytesLength <= 0 || bytesLength > MaxBytes)
                throw new ArgumentOutOfRangeException(nameof(bytesLength), $"El archivo del logo debe ser mayor que 0 y no exceder {MaxBytes} bytes.");

            if (anchoPx < MinAnchoPx || altoPx < MinAltoPx || anchoPx > MaxAnchoPx || altoPx > MaxAltoPx)
                throw new ArgumentOutOfRangeException(nameof(anchoPx), $"Dimensiones fuera de rango permitido. Ancho: {MinAnchoPx}–{MaxAnchoPx}px, Alto: {MinAltoPx}–{MaxAltoPx}px.");

            var ext = Path.GetExtension(fileName) ?? string.Empty;
            if (!_extPermitidas.Contains(ext))
                throw new ArgumentOutOfRangeException(nameof(fileName), $"Extensión no permitida: \"{ext}\". Use PNG o JPG.");

            if (!_mimePermitidos.Contains(contentType))
                throw new ArgumentOutOfRangeException(nameof(contentType), $"Content-Type no permitido: \"{contentType}\". Use image/png o image/jpeg.");

            var aspecto = (double)anchoPx / altoPx;
            if (aspecto < MinAspecto || aspecto > MaxAspecto)
                throw new ArgumentOutOfRangeException(nameof(anchoPx), "La relación de aspecto del logo es extrema. Use un logo más balanceado.");

            var extNormalizada = ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ? ".jpg" : ext.ToLowerInvariant();
            var nombreSeguro = SanitizarNombre(fileName, extNormalizada);

            return new LogoImagen(nombreSeguro, contentType, bytesLength, anchoPx, altoPx, extNormalizada);
        }

        /// <summary>
        /// Intenta crear desde los metadatos. Devuelve false si alguna validación falla.
        /// </summary>
        public static bool TryFromUpload(string? fileName, string? contentType, long bytesLength, int anchoPx, int altoPx, out LogoImagen? logo)
        {
            logo = null;
            try
            {
                if (fileName is null || contentType is null) return false;
                logo = FromUpload(fileName, contentType, bytesLength, anchoPx, altoPx);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ------------------- Helpers de render -------------------

        /// <summary>
        /// Calcula el tamaño de render proporcional para caber en un rectángulo (maxWidth x maxHeight).
        /// Nunca escala hacia arriba: si ya es más pequeño, devuelve el tamaño actual.
        /// </summary>
        public (int width, int height) FitIn(int maxWidthPx, int maxHeightPx)
        {
            if (maxWidthPx <= 0 || maxHeightPx <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxWidthPx), "Los límites deben ser mayores que cero.");

            var w = AnchoPx;
            var h = AltoPx;

            if (w <= maxWidthPx && h <= maxHeightPx) return (w, h);

            var scaleW = (double)maxWidthPx / w;
            var scaleH = (double)maxHeightPx / h;
            var scale = Math.Min(scaleW, scaleH);

            var newW = Math.Max(1, (int)Math.Round(w * scale));
            var newH = Math.Max(1, (int)Math.Round(h * scale));
            return (newW, newH);
        }

        /// <summary>
        /// Sugerencia rápida: render para cabecera PDF A4 (área aprox. 220x80 px a 96dpi/plantilla web).
        /// Ajusta estos números a tu plantilla real.
        /// </summary>
        public (int width, int height) FitCabeceraA4() => FitIn(220, 80);

        // ------------------- Igualdad por valor -------------------

        public override bool Equals(object? obj)
        {
            if (obj is not LogoImagen other) return false;
            return string.Equals(ContentType, other.ContentType, StringComparison.Ordinal)
                && string.Equals(Extension, other.Extension, StringComparison.Ordinal)
                && BytesLength == other.BytesLength
                && AnchoPx == other.AnchoPx
                && AltoPx == other.AltoPx
                && string.Equals(NombreArchivo, other.NombreArchivo, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + ContentType.GetHashCode(StringComparison.Ordinal);
                h = h * 31 + Extension.GetHashCode(StringComparison.Ordinal);
                h = h * 31 + BytesLength.GetHashCode();
                h = h * 31 + AnchoPx.GetHashCode();
                h = h * 31 + AltoPx.GetHashCode();
                h = h * 31 + NombreArchivo.GetHashCode(StringComparison.Ordinal);
                return h;
            }
        }

        public static bool operator ==(LogoImagen? left, LogoImagen? right)
            => left is null ? right is null : left.Equals(right);

        public static bool operator !=(LogoImagen? left, LogoImagen? right) => !(left == right);

        public override string ToString() => $"{NombreArchivo} ({AnchoPx}x{AltoPx}px, {ContentType})";

        // ------------------- Utilidades privadas -------------------

        private static string SanitizarNombre(string original, string extNormalizada)
        {
            // Mantener solo letras, dígitos, '-', '_', y punto. Reemplazar el resto por '_'.
            var name = Path.GetFileNameWithoutExtension(original);
            if (string.IsNullOrWhiteSpace(name)) name = "logo";

            var sb = new StringBuilder(name.Length);
            foreach (var c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.')
                    sb.Append(c);
                else
                    sb.Append('_');
            }

            // Evitar nombres absurdamente largos
            var safe = sb.ToString();
            if (safe.Length > 80) safe = safe.Substring(0, 80);

            return safe + extNormalizada;
        }
    }
}
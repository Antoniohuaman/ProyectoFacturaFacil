using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ComprobantesElectronicosBC.Domain.ValueObjects
{
    /// <summary>
    /// VO que encapsula el texto que se EMITE en la línea del comprobante (snapshot).
    /// - Se usa para PDF y para UBL 2.1 en: cac:InvoiceLine/cac:Item/cbc:Description (0..n)
    /// - No busca productos ni resuelve impuestos: solo texto “congelado” de la línea.
    /// Invariantes principales:
    ///   * Nombre: obligatorio, 1..200 chars (seguro para PDF/UBL).
    ///   * Detalle: opcional, 0..1000 chars (se trunca si excede).
    ///   * Normalización: trim, colapso de espacios y limpieza de control chars.
    /// Utilidades:
    ///   * ToUblDescriptions(): primera línea = Nombre; siguientes = líneas del Detalle.
    ///   * ToPdfSingleLine(): “Nombre — Detalle” (si hay detalle).
    ///   * ToPdfMultiLine(): "Nombre\n<detalle en múltiples líneas>"
    /// </summary>
    public sealed record DescripcionProducto
    {
        public const int MaxNombre = 200;
        public const int MaxDetalle = 1000;

        /// <summary>Texto corto representativo del ítem (título de la línea).</summary>
    public string Nombre { get; init; }

    /// <summary>Texto descriptivo opcional (observaciones, especificaciones, etc.).</summary>
    public string? Detalle { get; init; }

        private DescripcionProducto(string nombre, string? detalle)
        {
            Nombre = nombre;
            Detalle = detalle;
        }

        /// <summary>
        /// Fábrica principal. Aplica normalización y valida invariantes.
        /// </summary>
        public static DescripcionProducto Create(string? nombre, string? detalle = null)
        {
            var n = NormalizeNombre(nombre);
            var d = NormalizeDetalle(detalle);
            if (string.IsNullOrWhiteSpace(n))
                throw new ArgumentException("El nombre de la descripción es obligatorio.", nameof(nombre));

            if (n.Length > MaxNombre) n = n[..MaxNombre];
            if (d is not null && d.Length > MaxDetalle) d = d[..MaxDetalle];

            return new DescripcionProducto(n, d);
        }

        /// <summary>
        /// Crea desde un “nombre de catálogo” y un detalle libre opcional que el usuario edita en el formulario.
        /// </summary>
        public static DescripcionProducto FromCatalogName(string catalogName, string? detalleLibre = null)
            => Create(catalogName, detalleLibre);

        /// <summary>
        /// Devuelve una nueva instancia agregando (con salto de línea) texto extra al detalle.
        /// Se normaliza y se respeta el máximo de longitud total del detalle.
        /// </summary>
        public DescripcionProducto WithAppendedDetail(string extra)
        {
            var extraNorm = NormalizeDetalle(extra) ?? string.Empty;
            var current = Detalle ?? string.Empty;

            var joined = string.IsNullOrEmpty(current) ? extraNorm : $"{current}\n{extraNorm}";
            if (joined.Length > MaxDetalle) joined = joined[..MaxDetalle];

            return this with { Detalle = string.IsNullOrWhiteSpace(joined) ? null : joined };
        }

        /// <summary>
        /// Representación compacta de una sola línea pensada para PDFs tipo ticket o columnas estrechas.
        /// </summary>
        public string ToPdfSingleLine()
        {
            if (string.IsNullOrWhiteSpace(Detalle)) return Nombre;
            var detalleUnaLinea = CollapseSpaces(Detalle!.ReplaceLineEndings(" ").Trim());
            var merged = $"{Nombre} — {detalleUnaLinea}";
            return merged.Length <= (MaxNombre + 3 + MaxDetalle) ? merged : merged[..Math.Min(merged.Length, MaxNombre + 3 + MaxDetalle)];
        }

        /// <summary>
        /// Representación multi-línea “Nombre” + salto + detalle (si existe).
        /// </summary>
        public string ToPdfMultiLine()
            => string.IsNullOrWhiteSpace(Detalle) ? Nombre : $"{Nombre}\n{Detalle}";

        /// <summary>
        /// Mapeo a UBL: primer cbc:Description = Nombre; el resto = cada línea del Detalle (si lo hay).
        /// </summary>
        public IReadOnlyList<string> ToUblDescriptions()
        {
            var list = new List<string> { Nombre };
            if (!string.IsNullOrWhiteSpace(Detalle))
            {
                var lines = SplitLinesSafe(Detalle!);
                list.AddRange(lines.Where(l => !string.IsNullOrWhiteSpace(l)));
            }
            return list;
        }

        // -----------------------
        // Normalización y helpers
        // -----------------------

        private static string NormalizeNombre(string? s)
        {
            s = SanitizeText(s);
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            s = CollapseSpaces(s).Trim();
            if (s.Length > MaxNombre) s = s[..MaxNombre];
            return s;
        }

        private static string? NormalizeDetalle(string? s)
        {
            s = SanitizeText(s);
            if (string.IsNullOrWhiteSpace(s)) return null;
            // Permitimos saltos de línea en detalle para UBL (múltiples cbc:Description) / PDF multilinea
            // Limpiamos espacios en líneas y descartamos líneas vacías al final.
            var lines = SplitLinesSafe(s).Select(l => CollapseSpaces(l.Trim())).ToList();
            // Elimina líneas completamente vacías internas
            lines = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            if (lines.Count == 0) return null;

            var joined = string.Join('\n', lines);
            if (joined.Length > MaxDetalle) joined = joined[..MaxDetalle];
            return joined;
        }

        private static string SanitizeText(string? s)
        {
            if (s is null) return string.Empty;
            s = s.ReplaceLineEndings("\n").Trim();

            // Remueve caracteres de control no imprimibles excepto '\n' y '\t'
            // (evita problemas en XML/PDF). También elimina NULL chars.
            var cleaned = new char[s.Length];
            var idx = 0;
            foreach (var ch in s)
            {
                if (ch == '\n' || ch == '\t' || !char.IsControl(ch))
                {
                    if (ch != '\0') cleaned[idx++] = ch;
                }
            }
            return new string(cleaned, 0, idx);
        }

        private static string CollapseSpaces(string s)
        {
            // Colapsa runs de espacios y tabs a un solo espacio (conservando acentos/ñ).
            return Regex.Replace(s, "[ \\t]+", " ");
        }

        private static List<string> SplitLinesSafe(string s)
            => s.ReplaceLineEndings("\n").Split('\n').ToList();
    }
}

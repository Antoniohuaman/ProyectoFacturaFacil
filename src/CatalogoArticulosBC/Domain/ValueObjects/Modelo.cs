#nullable enable
using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    /// <summary>
    /// Modelo comercial del producto (p.ej. "XPS 13 9320", "iPhone 15 Pro", "VEGA").
    /// Inmutable. Igualdad por valor (case-insensitive) usando una forma normalizada.
    /// </summary>
    [DebuggerDisplay("{Value}")]
    public sealed record Modelo
    {
        /// <summary>Texto como lo verá el usuario (con espacios normalizados).</summary>
        public string Value { get; }

        /// <summary>Representación normalizada (mayúsculas invariantes) para igualdad/búsqueda.</summary>
        public string Normalized { get; }

        /// <summary>Longitud máxima permitida.</summary>
        public const int MaxLength = 60;

        // Letras (incluye acentos), números, espacio, punto, guion, guion_bajo, slash, +, paréntesis y #
        private static readonly Regex Allowed =
            new(@"^[A-Za-zÁÉÍÓÚÜÑáéíóúüñ0-9][A-Za-zÁÉÍÓÚÜÑáéíóúüñ0-9\s\.\-_\/\+\(\)#]*$",
                RegexOptions.Compiled);

        private Modelo(string value, string normalized)
        {
            Value      = value;
            Normalized = normalized;
        }

        /// <summary>
        /// Crea el VO validando formato y normalizando espacios/caso.
        /// </summary>
        public static Modelo From(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("El modelo es obligatorio.", nameof(text));

            var cleaned = CollapseSpaces(text.Trim());

            if (cleaned.Length > MaxLength)
                throw new ArgumentOutOfRangeException(nameof(text),
                    $"El modelo no puede superar {MaxLength} caracteres.");

            if (!Allowed.IsMatch(cleaned))
                throw new ArgumentException(
                    "El modelo solo admite letras, números, espacios y . - _ / + ( ) #",
                    nameof(text));

            var normalized = cleaned.ToUpperInvariant();
            return new Modelo(cleaned, normalized);
        }

        /// <summary>Versión segura que no lanza excepciones.</summary>
        public static bool TryFrom(string? text, out Modelo? modelo)
        {
            try { modelo = From(text ?? string.Empty); return true; }
            catch { modelo = null; return false; }
        }

        public override string ToString() => Value;

        // Igualdad por forma normalizada (insensible a mayúsculas/minúsculas)
        public bool Equals(Modelo? other) =>
            other is not null && Normalized == other.Normalized;

        public override int GetHashCode() =>
            Normalized.GetHashCode(StringComparison.Ordinal);

        // Conversión implícita a string para comodidad en vistas/DTOs
        public static implicit operator string(Modelo m) => m.Value;

        // (Opcional) Conversión explícita desde string para evitar parse silencioso
        public static explicit operator Modelo(string s) => From(s);

        // Normaliza espacios consecutivos a uno solo
        private static string CollapseSpaces(string s)
        {
            var sb = new StringBuilder(s.Length);
            var prevSpace = false;
            foreach (var ch in s)
            {
                if (char.IsWhiteSpace(ch))
                {
                    if (!prevSpace) { sb.Append(' '); prevSpace = true; }
                }
                else
                {
                    sb.Append(ch);
                    prevSpace = false;
                }
            }
            return sb.ToString();
        }
    }
}

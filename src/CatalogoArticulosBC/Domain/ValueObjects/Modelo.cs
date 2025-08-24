#nullable enable
using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    /// <summary>
    /// Modelo comercial del producto (p.ej. "XPS 13 9320", "iPhone 15 Pro", "VEGA").
    /// Inmutable.
    /// <para>
    /// <b>Regla de igualdad:</b> Dos modelos se consideran iguales si, tras normalizar:
    /// <list type="bullet">
    /// <item>Se ignoran mayúsculas y espacios extra.</item>
    /// <item>Se <b>respetan</b> acentos y caracteres especiales.</item>
    /// <item>Ejemplo: "CANCIÓN125" y "CANCION125" <b>no</b> son iguales.</item>
    /// </list>
    /// </para>
    /// </summary>
    [DebuggerDisplay("{Value}")]
    public sealed class Modelo
    {
    public string Value { get; }
    public string Normalized { get; }
        public const int MaxLength = 60;
        private static readonly Regex Allowed = new(@"^[A-Za-zÁÉÍÓÚÜÑáéíóúüñ0-9][A-Za-zÁÉÍÓÚÜÑáéíóúüñ0-9\s\.\-_\/\+\(\)#]*$", RegexOptions.Compiled);

        private Modelo(string value)
        {
            Value = Regex.Replace(value, @"\s+", " ").Trim();
            // Normalización: mayúsculas, colapsa espacios, respeta acentos y caracteres especiales
            Normalized = Regex.Replace(Value, @"\s+", " ").Trim().ToUpperInvariant();
        }

        public static Modelo From(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("El modelo es obligatorio.", nameof(text));

            var cleaned = Regex.Replace(text, @"\s+", " ").Trim();
            if (cleaned.Length > MaxLength)
                throw new ArgumentOutOfRangeException(nameof(text), $"El modelo no puede superar {MaxLength} caracteres.");
            if (!Allowed.IsMatch(cleaned))
                throw new ArgumentException("El modelo solo admite letras, números, espacios y . - _ / + ( ) #", nameof(text));
            return new Modelo(cleaned);
        }

        public override string ToString() => Value;

        public override bool Equals(object? obj)
        {
            if (obj is Modelo other)
                return Normalized == other.Normalized;
            return false;
        }

        public override int GetHashCode() => Normalized.GetHashCode(StringComparison.Ordinal);

        public static bool operator ==(Modelo? left, Modelo? right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Normalized == right.Normalized;
        }

            // Permite crear el VO sin lanzar excepción
            public static bool TryFrom(string? text, out Modelo? modelo)
            {
                try { modelo = From(text ?? string.Empty); return true; }
                catch { modelo = null; return false; }
            }

            // Conversión implícita a string
            public static implicit operator string(Modelo m) => m.Value;

            // Conversión explícita desde string
            public static explicit operator Modelo(string s) => From(s);

        public static bool operator !=(Modelo? left, Modelo? right) => !(left == right);
    }
}

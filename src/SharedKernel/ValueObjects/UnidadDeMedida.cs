#nullable enable
using System;
using System.Diagnostics;

namespace SharedKernel.ValueObjects
{
    /// <summary>
    /// VO Unidad de Medida (solo el CÓDIGO, normalizado a MAYÚSCULAS).
    /// Compatible con códigos SUNAT/UNECE (NIU, KGM, ZZ, C62, KWH, …)
    /// y también internos de la empresa (CAJA, MILLAR, …).
    /// No incluye tablas ni nombres: eso vive en ConfiguraciónSistemaBC.
    /// </summary>
    [DebuggerDisplay("{Codigo}")]
    public sealed record UnidadDeMedida
    {
        /// <summary>Código normalizado. Ej.: "NIU", "KGM", "ZZ", "C62", "CAJA".</summary>
        public string Codigo { get; }

        private const int MinLen = 1;
        private const int MaxLen = 15;

        private UnidadDeMedida(string codigo, Func<string, bool>? extraValidator)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                throw new ArgumentException("Código de unidad de medida obligatorio.", nameof(codigo));

            var norm = codigo.Trim().ToUpperInvariant();

            if (norm.Length < MinLen || norm.Length > MaxLen)
                throw new ArgumentOutOfRangeException(nameof(codigo),
                    $"El código debe tener entre {MinLen} y {MaxLen} caracteres.");

            // Solo [A–Z][0–9] y '-' '_'
            foreach (var ch in norm)
            {
                var ok = (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch == '-' || ch == '_';
                if (!ok)
                    throw new ArgumentException("El código solo admite letras, números, '-' o '_'.", nameof(codigo));
            }

            // Validación adicional opcional (p.ej., whitelist SUNAT) — úsala solo en Configuración.
            if (extraValidator is not null && !extraValidator(norm))
                throw new ArgumentException("Código no permitido por la política configurada.", nameof(codigo));

            Codigo = norm;
        }

        /// <summary>Crear con validación de formato (uso general en BC consumidores).</summary>
        public static UnidadDeMedida From(string codigo) => new(codigo, extraValidator: null);

        /// <summary>Crear validando contra una whitelist (p.ej., lista SUNAT) — usar en Configuración.</summary>
        public static UnidadDeMedida FromStrict(string codigo, Func<string, bool> allowed)
            => new(codigo, allowed ?? throw new ArgumentNullException(nameof(allowed)));

        /// <summary>Try sin excepciones (sin validador extra).</summary>
        public static bool TryFrom(string codigo, out UnidadDeMedida? unidad)
        {
            try { unidad = new UnidadDeMedida(codigo, null); return true; }
            catch { unidad = null; return false; }
        }

        // Atajos comunes (no sustituyen al catálogo)
        public static readonly UnidadDeMedida NIU = new("NIU", null); // UNIDAD
        public static readonly UnidadDeMedida KGM = new("KGM", null); // KILOGRAMO
        public static readonly UnidadDeMedida LTR = new("LTR", null); // LITRO
        public static readonly UnidadDeMedida MTR = new("MTR", null); // METRO
        public static readonly UnidadDeMedida ZZ  = new("ZZ",  null); // SERVICIO

        public override string ToString() => Codigo;

        // Conversiones convenientes
        public static implicit operator string(UnidadDeMedida u) => u.Codigo;
        public static explicit operator UnidadDeMedida(string codigo) => From(codigo);
    }
}

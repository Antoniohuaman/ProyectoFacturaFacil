using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ConfiguracionSistemaBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object para Moneda base/soportada por el sistema.
    /// - Igualdad por código ISO 4217 (p. ej., "PEN", "USD").
    /// - Este VO NO decide si es predeterminada ni si está activa; eso vive en el agregado de configuración.
    /// </summary>
    [DebuggerDisplay("{Codigo} ({Simbolo})")]
    public sealed class Moneda
    {
        // --------------------- Instancias soportadas ---------------------
        public static readonly Moneda PEN = new("PEN", "SOL", "S/.", decimales: 2, esNacional: true);
        public static readonly Moneda USD = new("USD", "DÓLAR ESTADOUNIDENSE", "US$", decimales: 2, esNacional: false);

        public static IReadOnlyCollection<Moneda> All => _byCode.Values;

        // --------------------- Estado inmutable ---------------------
        /// <summary>Código ISO 4217 alfabético (p. ej., "PEN").</summary>
        public string Codigo { get; }

        /// <summary>Nombre corto descriptivo (para UI).</summary>
        public string Nombre { get; }

        /// <summary>Símbolo habitual (p. ej., "S/.", "US$").</summary>
        public string Simbolo { get; }

        /// <summary>Cantidad de decimales usada en importes.</summary>
        public int Decimales { get; }

        /// <summary>True si es la moneda nacional (en nuestro contexto, PEN).</summary>
        public bool EsMonedaNacional { get; }

        public bool EsPen => ReferenceEquals(this, PEN);
        public bool EsUsd => ReferenceEquals(this, USD);

        private Moneda(string codigo, string nombre, string simbolo, int decimales, bool esNacional)
        {
            if (!EsCodigoIsoValido(codigo))
                throw new ArgumentOutOfRangeException(nameof(codigo), "El código de moneda debe ser ISO-4217 de 3 letras (A–Z).");

            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentNullException(nameof(nombre));

            if (string.IsNullOrWhiteSpace(simbolo))
                throw new ArgumentNullException(nameof(simbolo));

            if (decimales < 0 || decimales > 4)
                throw new ArgumentOutOfRangeException(nameof(decimales), "Los decimales deben estar entre 0 y 4.");

            Codigo = codigo;
            Nombre = nombre.Trim();
            Simbolo = simbolo.Trim();
            Decimales = decimales;
            EsMonedaNacional = esNacional;
        }

        // --------------------- Fábricas / Parseo ---------------------
        private static readonly Dictionary<string, Moneda> _byCode =
            new(StringComparer.Ordinal)
            {
                ["PEN"] = PEN,
                ["USD"] = USD
            };

        // Aliases para entrada humana
        private static readonly Dictionary<string, string> _aliasToCode =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // PEN
                ["PEN"] = "PEN",
                ["S/"] = "PEN",
                ["S/."] = "PEN",
                ["SOL"] = "PEN",
                ["SOLES"] = "PEN",
                ["NUEVO SOL"] = "PEN",

                // USD
                ["USD"] = "USD",
                ["US$"] = "USD",
                ["DOLAR"] = "USD",
                ["DÓLAR"] = "USD",
                ["DOLARES"] = "USD",
                ["DÓLARES"] = "USD",
                ["DOLLAR"] = "USD"
            };

        /// <summary>Crea desde código ISO soportado ("PEN" / "USD").</summary>
        public static Moneda FromCode(string codigoIso)
        {
            if (string.IsNullOrWhiteSpace(codigoIso))
                throw new ArgumentNullException(nameof(codigoIso));

            var key = codigoIso.Trim().ToUpperInvariant();
            if (_byCode.TryGetValue(key, out var m)) return m;

            throw new ArgumentOutOfRangeException(nameof(codigoIso), $"Moneda no soportada: \"{codigoIso}\". Use PEN o USD.");
        }

        /// <summary>Crea desde código o alias humano (p. ej., "S/.", "DÓLAR").</summary>
        public static Moneda From(string codigoOAlias)
        {
            if (!TryParse(codigoOAlias, out var m))
                throw new ArgumentOutOfRangeException(nameof(codigoOAlias), $"Moneda no reconocida: \"{codigoOAlias}\".");
            return m!;
        }

        /// <summary>Intenta parsear desde código o alias. False si no se reconoce.</summary>
        public static bool TryParse(string? codigoOAlias, out Moneda? moneda)
        {
            moneda = null;
            if (string.IsNullOrWhiteSpace(codigoOAlias)) return false;

            var key = codigoOAlias.Trim();
            if (_byCode.TryGetValue(key.ToUpperInvariant(), out moneda)) return true;

            if (_aliasToCode.TryGetValue(key, out var canonical))
            {
                moneda = _byCode[canonical];
                return true;
            }
            return false;
        }

        // --------------------- Helpers de dominio ---------------------

        /// <summary>
        /// Verifica si un monto tiene una precisión de decimales válida para esta moneda.
        /// Ej.: con 2 decimales: 10.00 ok, 10.123 inválido.
        /// </summary>
        public bool TienePrecisionValida(decimal monto)
        {
            return CuentaDecimales(monto) <= Decimales;
        }

        private static int CuentaDecimales(decimal value)
        {
            // Representación interna: escala = (int)((value & 0x00FF0000) >> 16) en decimal.GetBits
            var bits = decimal.GetBits(value);
            int scale = (bits[3] >> 16) & 0x7F;
            if (scale < 0) scale = 0;
            return scale;
        }

        // --------------------- Igualdad por valor ---------------------
        public override bool Equals(object? obj)
            => obj is Moneda other && string.Equals(Codigo, other.Codigo, StringComparison.Ordinal);

        public override int GetHashCode() => Codigo.GetHashCode(StringComparison.Ordinal);

        public static bool operator ==(Moneda? left, Moneda? right)
            => left is null ? right is null : left.Equals(right);

        public static bool operator !=(Moneda? left, Moneda? right) => !(left == right);

        public override string ToString() => Codigo;

        public static implicit operator string(Moneda value) => value.Codigo;

        public static explicit operator Moneda(string value) => From(value);

        // --------------------- Validación básica ---------------------
        private static bool EsCodigoIsoValido(string? codigo)
        {
            if (codigo is null || codigo.Length != 3) return false;
            for (int i = 0; i < 3; i++)
            {
                char c = codigo[i];
                if (c < 'A' || c > 'Z') return false;
            }
            return true;
        }
    }
}
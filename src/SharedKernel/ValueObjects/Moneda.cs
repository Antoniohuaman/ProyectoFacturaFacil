using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SharedKernel.ValueObjects
{
    /// <summary>
    /// Value Object de Moneda conforme a ISO-4217 (relevante para UBL/SUNAT: currencyID).
    /// - Igualdad por valor.
    /// - Código siempre MAYÚSCULAS y de 3 letras (ej.: "PEN", "USD").
    /// - Decimales permitidos: 0..4 (PEN y USD usan 2).
    /// </summary>
    [DebuggerDisplay("{Codigo} ({Simbolo}), Decimales={Decimales}")]
    public sealed record Moneda
    {
        /// <summary> Código ISO-4217: "PEN", "USD", etc. </summary>
        public string Codigo { get; init; }

        /// <summary> Símbolo visible: "S/", "$", etc. (para impresión/UX). </summary>
        public string Simbolo { get; init; }

        /// <summary> Cantidad de decimales para redondeo y formateo (0..4). </summary>
        public byte Decimales { get; init; }

        private const byte MAX_DECIMALES = 4;
        private static readonly Regex _isoCode = new(@"^[A-Za-z]{3}$", RegexOptions.Compiled);

        // Defaults útiles en Perú (amplía cuando habilites más monedas).
        private static readonly Dictionary<string, (string simbolo, byte decimales)> _defaults =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["PEN"] = ("S/.", 2),
                ["USD"] = ("$", 2),
                // ["EUR"] = ("€", 2),
            };

        // Constructor privado: usa fábricas para crear instancias válidas.
        private Moneda(string codigo, string simbolo, byte decimales)
        {
            Codigo = codigo;
            Simbolo = simbolo;
            Decimales = decimales;
        }

        /// <summary>
        /// Fábrica principal. Valida ISO-4217 y rango de decimales.
        /// Si no indicas símbolo/decimales y el código existe en defaults, aplica los valores conocidos.
        /// </summary>
        public static Moneda Create(string codigo, string? simbolo = null, byte? decimales = null)
        {
            if (string.IsNullOrWhiteSpace(codigo) || !_isoCode.IsMatch(codigo.Trim()))
                throw new ArgumentException("El código de moneda debe ser ISO-4217 de 3 letras.", nameof(codigo));

            var iso = codigo.Trim().ToUpperInvariant();

            if (!simbolo.HasValueEx() && !decimales.HasValue && _defaults.TryGetValue(iso, out var def))
                return new Moneda(iso, def.simbolo, def.decimales);

            var sym = simbolo.HasValueEx() ? simbolo!.Trim() : iso;
            var dec = decimales ?? 2;

            if (dec > MAX_DECIMALES)
                throw new ArgumentOutOfRangeException(nameof(decimales), $"Los decimales permitidos son 0..{MAX_DECIMALES}.");

            return new Moneda(iso, sym, dec);
        }

        /// <summary> Fábrica rápida: Sol peruano (PEN, "S/", 2). </summary>
        public static Moneda PEN() => new("PEN", "S/.", 2);

        /// <summary> Fábrica rápida: Dólar estadounidense (USD, "$", 2). </summary>
        public static Moneda USD() => new("USD", "$", 2);

        /// <summary>
        /// TryCreate no lanza excepciones. Devuelve false si el código no es ISO-4217 (3 letras)
        /// o los decimales están fuera del rango permitido.
        /// </summary>
        public static bool TryCreate(string codigo, out Moneda? moneda, string? simbolo = null, byte? decimales = null)
        {
            moneda = null;

            if (string.IsNullOrWhiteSpace(codigo) || !_isoCode.IsMatch(codigo.Trim()))
                return false;

            var iso = codigo.Trim().ToUpperInvariant();

            if (!simbolo.HasValueEx() && !decimales.HasValue && _defaults.TryGetValue(iso, out var def))
            {
                moneda = new Moneda(iso, def.simbolo, def.decimales);
                return true;
            }

            var sym = simbolo.HasValueEx() ? simbolo!.Trim() : iso;
            var dec = decimales ?? 2;

            if (dec > MAX_DECIMALES)
                return false;

            moneda = new Moneda(iso, sym, dec);
            return true;
        }
    }

    internal static class StringExtensions
    {
        public static bool HasValueEx(this string? s) => !string.IsNullOrWhiteSpace(s);
    }
}

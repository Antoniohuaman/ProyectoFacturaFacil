using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace ComprobantesElectronicosBC.Domain.ValueObjects
{
    /// <summary>
    /// VO para una dirección de correo válida.
    /// - Inmutable, igualdad por valor.
    /// - Normaliza: recorta y pone el dominio en minúsculas (IDN → ASCII).
    /// - Valida longitudes aprox. RFC: local ≤ 64, dominio ≤ 255, total ≤ 254.
    /// - Helpers para lista opcional (0..5) o lista obligatoria (1..5).
    /// </summary>
    public sealed record Email
    {
        public const int MaxDestinatarios = 5;

        public string Value { get; }

        private Email(string canonical) => Value = canonical;

        public string LocalPart => Value[..Value.IndexOf('@')];
        public string Domain    => Value[(Value.IndexOf('@') + 1)..];

        public override string ToString() => Value;

        public static Email Create(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("El email no puede ser vacío.", nameof(input));

            var raw = input.Trim().Trim('<', '>', '"', '\''); // quita envolturas comunes

            MailAddress addr;
            try { addr = new MailAddress(raw); }
            catch (Exception ex)
            {
                throw new ArgumentException($"Formato de email inválido: {input}", nameof(input), ex);
            }

            // queremos SOLO la dirección (sin “Nombre <…>”)
            if (!string.Equals(addr.Address, raw, StringComparison.Ordinal))
                throw new ArgumentException("Use solo la dirección (sin nombre para mostrar).", nameof(input));

            var address = addr.Address;
            var at = address.LastIndexOf('@');
            if (at <= 0 || at == address.Length - 1)
                throw new ArgumentException("El email debe contener parte local y dominio.", nameof(input));

            var local  = address[..at];
            var domain = address[(at + 1)..];

            // IDN -> ASCII (punycode) + minúsculas
            var idn = new IdnMapping();
            string asciiDomain;
            try { asciiDomain = idn.GetAscii(domain).ToLowerInvariant(); }
            catch (Exception ex)
            {
                throw new ArgumentException("Dominio inválido (IDN).", nameof(input), ex);
            }

            if (local.Length is < 1 or > 64)
                throw new ArgumentException("La parte local debe tener 1..64 caracteres.", nameof(input));
            if (asciiDomain.Length is < 1 or > 255)
                throw new ArgumentException("El dominio debe tener 1..255 caracteres.", nameof(input));

            var labels = asciiDomain.Split('.');
            foreach (var label in labels)
                if (!IsValidDnsLabel(label))
                    throw new ArgumentException("El dominio contiene etiquetas inválidas.", nameof(input));
            if (labels[^1].Length < 2)
                throw new ArgumentException("El TLD debe tener al menos 2 caracteres.", nameof(input));

            var canonical = $"{local}@{asciiDomain}";
            if (canonical.Length > 254)
                throw new ArgumentException("El email no debe exceder 254 caracteres.", nameof(input));

            return new Email(canonical);
        }

        public static bool TryCreate(string? input, out Email? email)
        {
            try { email = Create(input!); return true; }
            catch { email = null; return false; }
        }

        /// <summary>Lista OBLIGATORIA: debe devolver 1..maxCount correos.</summary>
        public static IReadOnlyList<Email> ParseList(string raw, int maxCount = MaxDestinatarios)
        {
            var list = ParseListOrEmpty(raw, maxCount);
            if (list.Count == 0)
                throw new ArgumentException("Proporcione al menos un email.", nameof(raw));
            return list;
        }

        /// <summary>
        /// Lista OPCIONAL: si raw es vacío/null, devuelve lista vacía.
        /// Útil para tu formulario (0..maxCount).
        /// </summary>
        public static IReadOnlyList<Email> ParseListOrEmpty(string? raw, int maxCount = MaxDestinatarios)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return Array.Empty<Email>();

            // ✅ Usa arreglo + StringSplitOptions (evita el error de conversión a 'char')
            var separators = new[] { ',', ';', ' ', '\n', '\r', '\t' };
            var tokens = raw.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            var result = new List<Email>(Math.Min(tokens.Length, maxCount));
            foreach (var t in tokens)
            {
                if (result.Count >= maxCount)
                    throw new ArgumentException($"No se permiten más de {maxCount} correos.");

                Email e = Create(t);              // no null (lanza si es inválido)
                if (!result.Contains(e)) result.Add(e);
            }

            return result.AsReadOnly();
        }

        public static bool TryParseListOrEmpty(string? raw, out IReadOnlyList<Email> emails, int maxCount = MaxDestinatarios)
        {
            try { emails = ParseListOrEmpty(raw, maxCount); return true; }
            catch { emails = Array.Empty<Email>(); return false; }
        }

        // 🔧 Helper PRIVADO: válido para etiquetas DNS (dentro del dominio)
        private static bool IsValidDnsLabel(string label)
        {
            if (label.Length is < 1 or > 63) return false;
            if (label.StartsWith('-') || label.EndsWith('-')) return false;
            return Regex.IsMatch(label, "^[A-Za-z0-9-]+$");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace SharedKernel.ValueObjects
{
    /// <summary>
    /// Value Object para una dirección de correo electrónico válida y normalizada.
    /// - Inmutable, igualdad por valor.
    /// - Normaliza: recorta y pone el dominio en minúsculas (IDN → ASCII).
    /// - Valida longitudes según RFC: local ≤ 64, dominio ≤ 255, total ≤ 254.
    /// - Permite hasta 5 correos por entidad (cliente, comprobante, etc).
    /// - Métodos para crear, validar y parsear listas de correos (obligatoria y opcional).
    /// - Útil para compartir entre GestiónClientesBC y ComprobantesElectronicosBC.
    /// - Permite autocompletar, agregar, eliminar y validar correos en formularios.
    /// - Puede usarse para guardar correos en ficha de cliente y para envío automático en comprobantes.
    /// </summary>
    public sealed record Email
    {
        /// <summary>
        /// Máximo de destinatarios permitidos por entidad (cliente, comprobante, etc).
        /// </summary>
        public const int MaxDestinatarios = 5;

        /// <summary>
        /// Valor normalizado del correo electrónico.
        /// </summary>
        public string Value { get; }

        private Email(string canonical) => Value = canonical;

        /// <summary>
        /// Parte local del correo (antes del @).
        /// </summary>
        public string LocalPart => Value[..Value.IndexOf('@')];
        /// <summary>
        /// Dominio del correo (después del @).
        /// </summary>
        public string Domain    => Value[(Value.IndexOf('@') + 1)..];

        public override string ToString() => Value;

        /// <summary>
        /// Crea y valida un correo electrónico. Lanza excepción si es inválido.
        /// </summary>
        public static Email Create(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("El email no puede ser vacío.", nameof(input));

            var raw = input.Trim().Trim('<', '>', '"', '\''); // quita envolturas comunes


            // Validación básica: debe tener exactamente un '@', partes no vacías y sin espacios
            if (raw.Count(c => c == '@') != 1)
                throw new ArgumentException("El email debe contener exactamente un '@'.", nameof(input));
            if (raw.Contains(" "))
                throw new ArgumentException("El email no debe contener espacios.", nameof(input));
            var at = raw.LastIndexOf('@');
            if (at <= 0 || at == raw.Length - 1)
                throw new ArgumentException("El email debe contener parte local y dominio.", nameof(input));
            var local = raw[..at];
            var domain = raw[(at + 1)..];
            if (string.IsNullOrWhiteSpace(local))
                throw new ArgumentException("La parte local no puede estar vacía.", nameof(input));
            if (string.IsNullOrWhiteSpace(domain))
                throw new ArgumentException("El dominio no puede estar vacío.", nameof(input));
            if (local.Contains(" ") || domain.Contains(" "))
                throw new ArgumentException("La parte local y el dominio no deben contener espacios.", nameof(input));
            if (local.Contains("@") || domain.Contains("@"))
                throw new ArgumentException("La parte local y el dominio no deben contener '@'.", nameof(input));

            // Validación con MailAddress para formato general
            MailAddress addr;
            try { addr = new MailAddress(raw); }
            catch (Exception ex)
            {
                throw new ArgumentException($"Formato de email inválido: {input}", nameof(input), ex);
            }

            // Solo dirección, sin nombre para mostrar
            if (!string.Equals(addr.Address, raw, StringComparison.Ordinal))
                throw new ArgumentException("Use solo la dirección (sin nombre para mostrar).", nameof(input));

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
            if (labels.Length < 2)
                throw new ArgumentException("El dominio debe contener al menos un punto (ejemplo: dominio.com).", nameof(input));
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

        /// <summary>
        /// Intenta crear y validar un correo electrónico. Devuelve true si es válido.
        /// </summary>
        public static bool TryCreate(string? input, out Email? email)
        {
            try { email = Create(input!); return true; }
            catch { email = null; return false; }
        }

        /// <summary>
        /// Parsea una lista OBLIGATORIA de correos (debe devolver 1..maxCount correos).
        /// </summary>
        public static IReadOnlyList<Email> ParseList(string raw, int maxCount = MaxDestinatarios)
        {
            var list = ParseListOrEmpty(raw, maxCount);
            if (list.Count == 0)
                throw new ArgumentException("Proporcione al menos un email.", nameof(raw));
            return list;
        }

        /// <summary>
        /// Parsea una lista OPCIONAL de correos (devuelve lista vacía si no hay datos).
        /// Útil para formularios donde el usuario puede agregar, quitar o digitar correos.
        /// </summary>
        public static IReadOnlyList<Email> ParseListOrEmpty(string? raw, int maxCount = MaxDestinatarios)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return Array.Empty<Email>();

            // Separadores comunes: coma, punto y coma, espacio, salto de línea, tabulación
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

        /// <summary>
        /// Intenta parsear una lista opcional de correos. Devuelve true si todos son válidos.
        /// </summary>
        public static bool TryParseListOrEmpty(string? raw, out IReadOnlyList<Email> emails, int maxCount = MaxDestinatarios)
        {
            try { emails = ParseListOrEmpty(raw, maxCount); return true; }
            catch { emails = Array.Empty<Email>(); return false; }
        }

        /// <summary>
        /// Valida una etiqueta DNS (parte del dominio).
        /// </summary>
        private static bool IsValidDnsLabel(string label)
        {
            if (label.Length is < 1 or > 63) return false;
            if (label.StartsWith('-') || label.EndsWith('-')) return false;
            return Regex.IsMatch(label, "^[A-Za-z0-9-]+$");
        }
    }
}

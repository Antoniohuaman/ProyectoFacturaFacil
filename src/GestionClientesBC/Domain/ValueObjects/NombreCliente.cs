using System;
using System.Linq;
using System.Text.RegularExpressions;
using SharedKernel.Exceptions; // ajusta si tu excepción vive en otro namespace

namespace GestionClientesBC.Domain.ValueObjects
{
    /// <summary>
    /// Nombre de cliente (persona natural o razón social).
    /// Cumple SUNAT: longitud 1..100 tras normalizar (trim y colapso de espacios).
    /// - Entrada MANUAL: valida longitud y permite caracteres comunes en razones sociales:
    ///   letras Unicode, dígitos, espacios y . , & ' / - ( ) : ; °
    ///   Para mostrar: capitaliza y fuerza siglas societarias (S.A., S.A.C., S.R.L., E.I.R.L., etc.) a MAYÚSCULAS.
    /// - Entrada OFICIAL (RENIEC/SUNAT): usa CrearDesdeFuenteOficial para conservar literal
    ///   (solo trim + colapso de espacios y validación de longitud).
    /// - Igualdad por valor usando representación canónica (upper + espacios colapsados).
    /// </summary>
    public sealed class NombreCliente : IEquatable<NombreCliente>
    {
        // Regex permisivo para entrada manual (evita @ # * =, etc., pero no restringe en exceso)
        private static readonly Regex AllowedManual = new(
            @"^[\p{L}\p{Mn}\p{Nd}\p{Zs}\.\,&'\/\-\(\):;°]+$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // Colapsa 2+ espacios en uno
        private static readonly Regex MultiSpace = new(@"\s{2,}", RegexOptions.Compiled);

        // Siglas societarias frecuentes (con/sin puntos)
        private static readonly System.Collections.Generic.HashSet<string> SiglasSocietarias =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "S.A.", "S.A.A.", "S.A.C.", "S.R.L.", "E.I.R.L.", "S.A.C.S.",
                "SA", "SAA", "SAC", "SRL", "EIRL", "SACS"
            };

        /// <summary>Representación canónica: MAYÚSCULAS + espacios colapsados (para igualdad/comparación).</summary>
        public string Valor { get; }

        /// <summary>Texto para mostrar (capitalizado, con siglas societarias en MAYÚSCULAS).</summary>
        public string ParaMostrar { get; }

        // EF Core
        private NombreCliente() { Valor = null!; ParaMostrar = null!; }

        private NombreCliente(string canonico, string display)
        {
            Valor = canonico;
            ParaMostrar = display;
        }

        /// <summary>
        /// Crear desde entrada MANUAL del usuario.
        /// Valida longitud 1..100, colapsa espacios, permite caracteres comunes y
        /// aplica capitalización + siglas societarias en MAYÚSCULAS para mostrar.
        /// </summary>
        public static NombreCliente Crear(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                throw new BusinessRuleException("El nombre del cliente no puede estar vacío.");

            var trimmed = MultiSpace.Replace(texto.Trim(), " ");

            // SUNAT: an..100
            if (trimmed.Length < 1 || trimmed.Length > 100)
                throw new BusinessRuleException("El nombre debe tener entre 1 y 100 caracteres.");

            if (!AllowedManual.IsMatch(trimmed))
                throw new BusinessRuleException("El nombre contiene caracteres no permitidos.");

            if (!trimmed.Any(char.IsLetterOrDigit))
                throw new BusinessRuleException("El nombre debe contener letras o dígitos.");

            var canonico = trimmed.ToUpperInvariant();      // para igualdad/búsqueda
            var display  = CapitalizarParaMostrar(trimmed); // para UI/impresión

            return new NombreCliente(canonico, display);
        }

        /// <summary>
        /// Crear desde fuente OFICIAL (RENIEC/SUNAT). Conserva literal (no filtra caracteres),
        /// solo trim/colapso de espacios y valida longitud 1..100.
        /// </summary>
        public static NombreCliente CrearDesdeFuenteOficial(string? textoOficial)
        {
            if (string.IsNullOrWhiteSpace(textoOficial))
                throw new BusinessRuleException("El nombre oficial no puede estar vacío.");

            var display = MultiSpace.Replace(textoOficial.Trim(), " ");

            if (display.Length < 1 || display.Length > 100)
                throw new BusinessRuleException("El nombre oficial debe tener entre 1 y 100 caracteres.");

            var canonico = display.ToUpperInvariant();
            return new NombreCliente(canonico, display);
        }

        public static bool TryCrear(string? texto, out NombreCliente? nombre)
        {
            try { nombre = Crear(texto); return true; }
            catch { nombre = null; return false; }
        }

        public override string ToString() => ParaMostrar;

        #region Igualdad por valor
        public bool Equals(NombreCliente? other) => other is not null && Valor == other.Valor;
        public override bool Equals(object? obj) => obj is NombreCliente n && Equals(n);
        public override int GetHashCode() => Valor.GetHashCode(StringComparison.Ordinal);
        #endregion

        #region Helper de capitalización (para MANUAL)
        private static string CapitalizarParaMostrar(string s)
        {
            var culture = System.Globalization.CultureInfo.GetCultureInfo("es-PE");
            var words = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < words.Length; i++)
            {
                var w = words[i];

                // Normalizaciones para detección
                string wUpper = w.ToUpperInvariant();
                string wNoDotsUpper = wUpper.Replace(".", "");

                // 1) Forzar siglas societarias conocidas (con o sin puntos)
                if (SiglasSocietarias.Contains(wUpper) || SiglasSocietarias.Contains(wNoDotsUpper))
                {
                    // Subir únicamente letras a mayúsculas, conservar puntuación/dígitos
                    words[i] = Regex.Replace(w, @"\p{L}", m => m.Value.ToUpperInvariant());
                    continue;
                }

                // 2) Acrónimos genéricos: MAYÚS cortas (≤5) o con puntos intercalados tipo S.A.C.
                bool hasDots = w.Contains('.') && w.Replace(".", "").All(char.IsLetter);
                var stripped = w.Replace(".", "");
                bool onlyLetters = stripped.Length > 0 && stripped.All(char.IsLetter);
                bool allUpperShort = onlyLetters && stripped == stripped.ToUpperInvariant() && stripped.Length <= 5;

                if (hasDots || allUpperShort)
                {
                    words[i] = w.ToUpperInvariant();
                }
                else
                {
                    // 3) Resto: TitleCase
                    var lower = w.ToLower(culture);
                    words[i] = culture.TextInfo.ToTitleCase(lower);
                }
            }

            return string.Join(' ', words);
        }
        #endregion
    }
}

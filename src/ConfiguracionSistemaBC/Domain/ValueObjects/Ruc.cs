using System;
using System.Diagnostics;

namespace ConfiguracionSistemaBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object para RUC peruano (Registro Único de Contribuyentes).
    /// Reglas (estrictas, práctica vigente):
    /// - 11 dígitos numéricos.
    /// - Prefijo permitido: 10 (PN con negocio) o 20 (PJ).
    /// - Dígito verificador válido (algoritmo SUNAT).
    ///
    /// Nota: la obtención de datos (razón social, domicilio, etc.) es responsabilidad de otra capa/BC.
    /// </summary>
    [DebuggerDisplay("{Numero}")]
    public sealed class Ruc
    {
        /// <summary>Código SUNAT para “RUC” como tipo de documento (cat. 6).</summary>
        public const string SunatDocumentTypeCode = "6";

        /// <summary>Valor canónico: exactamente 11 dígitos.</summary>
        public string Numero { get; }

        public string Base10 => Numero.Substring(0, 10);
        public int    DigitoVerificador => Numero[10] - '0';
        public string Prefijo => Numero.Substring(0, 2);

        public bool EsPersonaJuridica => Prefijo == "20";
        public bool EsPersonaNaturalConNegocio => Prefijo == "10";

        private Ruc(string numeroOnceDigitos) => Numero = numeroOnceDigitos;

        /// <summary>
        /// Crea desde texto libre (admite espacios/guiones), normaliza a 11 dígitos y valida:
        /// longitud, prefijo (10/20) y dígito verificador.
        /// </summary>
        public static Ruc FromString(string raw)
        {
            if (raw is null) throw new ArgumentNullException(nameof(raw));

            var digits = ExtraerSoloDigitos(raw);
            if (digits.Length != 11)
                throw new ArgumentOutOfRangeException(nameof(raw), "El RUC debe tener exactamente 11 dígitos.");

            if (!EsPrefijoPermitido(digits))
                throw new ArgumentOutOfRangeException(nameof(raw), "El RUC debe iniciar con 10 (PN) o 20 (PJ).");

            if (!ValidaDigitoVerificador(digits))
                throw new ArgumentException("El dígito verificador del RUC no es válido.", nameof(raw));

            return new Ruc(digits);
        }

        /// <summary>Intenta crear un RUC válido; false si falla la validación.</summary>
        public static bool TryFrom(string? raw, out Ruc? ruc)
        {
            ruc = null;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            var digits = ExtraerSoloDigitos(raw);
            if (digits.Length != 11) return false;
            if (!EsPrefijoPermitido(digits)) return false;
            if (!ValidaDigitoVerificador(digits)) return false;

            ruc = new Ruc(digits);
            return true;
        }

        /// <summary>Valida rápidamente formato + dígito verificador.</summary>
        public static bool EsValido(string? raw) => TryFrom(raw, out _);

        // -------- Igualdad por valor (basada en Numero) --------
        public override bool Equals(object? obj)
            => obj is Ruc other && string.Equals(Numero, other.Numero, StringComparison.Ordinal);

        public override int GetHashCode()
            => Numero.GetHashCode(StringComparison.Ordinal);

        public static bool operator ==(Ruc? left, Ruc? right)
            => left is null ? right is null : left.Equals(right);

        public static bool operator !=(Ruc? left, Ruc? right)
            => !(left == right);

        public override string ToString() => Numero;

        /// <summary>Implícito a string (retorna los 11 dígitos).</summary>
        public static implicit operator string(Ruc value) => value.Numero;

        /// <summary>Explícito desde string (normaliza y valida).</summary>
        public static explicit operator Ruc(string raw) => FromString(raw);

        // ---------------- Helpers internos ----------------
        private static string ExtraerSoloDigitos(string s)
        {
            var span = s.AsSpan();
            var buffer = new char[span.Length];
            var idx = 0;

            for (int i = 0; i < span.Length; i++)
            {
                var c = span[i];
                if (c >= '0' && c <= '9') buffer[idx++] = c;
            }
            return new string(buffer, 0, idx);
        }

        private static bool EsPrefijoPermitido(string digits11)
        {
            // Solo 10 o 20
            var p0 = digits11[0];
            var p1 = digits11[1];
            return (p0 == '1' && p1 == '0') || (p0 == '2' && p1 == '0');
        }

        /// <summary>
        /// Cálculo del dígito verificador (ponderadores 5,4,3,2,7,6,5,4,3,2).
        /// </summary>
        private static bool ValidaDigitoVerificador(string digits11)
        {
            int dvEsperado = digits11[10] - '0';
            int suma =
                (digits11[0] - '0') * 5 +
                (digits11[1] - '0') * 4 +
                (digits11[2] - '0') * 3 +
                (digits11[3] - '0') * 2 +
                (digits11[4] - '0') * 7 +
                (digits11[5] - '0') * 6 +
                (digits11[6] - '0') * 5 +
                (digits11[7] - '0') * 4 +
                (digits11[8] - '0') * 3 +
                (digits11[9] - '0') * 2;

            int resto = suma % 11;
            int dv = 11 - resto;
            if (dv == 10) dv = 0;
            else if (dv == 11) dv = 1;

            return dv == dvEsperado;
        }
    }
}
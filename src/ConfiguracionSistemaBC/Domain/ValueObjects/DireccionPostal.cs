using System;
using System.Diagnostics;

namespace ConfiguracionSistemaBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object para Dirección Postal conforme a requisitos SUNAT/UBL:
    /// - Ubigeo (6 dígitos)
    /// - Departamento / Provincia / Distrito
    /// - AddressTypeCode (4 dígitos, p.ej. "0000")
    /// - País (PE)
    /// - Línea de dirección textual completa (tal cual se ingresa; no se normalizan espacios)
    ///
    /// NOTA: La coherencia Ubigeo ↔ Departamento/Provincia/Distrito contra catálogos oficiales
    /// debe verificarse fuera (servicio/catálogo). Este VO valida formato y campos obligatorios.
    /// </summary>
    [DebuggerDisplay("{Linea}, {Distrito} - {Provincia} - {Departamento} ({Ubigeo})")]
    public sealed class DireccionPostal
    {
        /// <summary>Código ISO 3166-1 alpha-2 del país. En el contexto SUNAT: siempre "PE".</summary>
        public string PaisCodigoIso { get; }

        /// <summary>Ubigeo de 6 dígitos (no "000000").</summary>
        public string Ubigeo { get; }

        /// <summary>Departamento (tal cual se digitó/obtuvo; se conserva el espaciado).</summary>
        public string Departamento { get; }

        /// <summary>Provincia (tal cual se digitó/obtuvo; se conserva el espaciado).</summary>
        public string Provincia { get; }

        /// <summary>Distrito (tal cual se digitó/obtuvo; se conserva el espaciado).</summary>
        public string Distrito { get; }

        /// <summary>AddressTypeCode UBL (4 dígitos). Por defecto "0000" si no se especifica.</summary>
        public string AddressTypeCode { get; }

        /// <summary>Línea textual completa de dirección (p. ej. "AV. GIRÁLDEZ NRO. 634 URB. CERCADO").</summary>
        public string Linea { get; }

        private DireccionPostal(
            string paisCodigoIso,
            string ubigeo,
            string departamento,
            string provincia,
            string distrito,
            string addressTypeCode,
            string linea)
        {
            // Validaciones (no se alteran espacios; solo se verifica que no sean vacíos "en blanco")
            if (!EsPaisValido(paisCodigoIso))
                throw new ArgumentOutOfRangeException(nameof(paisCodigoIso), "El país debe ser 'PE'.");

            if (!EsUbigeoValido(ubigeo))
                throw new ArgumentOutOfRangeException(nameof(ubigeo), "Ubigeo inválido. Debe tener 6 dígitos y no ser '000000'.");

            if (!EsTextoObligatorio(departamento, maxLen: 80))
                throw new ArgumentOutOfRangeException(nameof(departamento), "Departamento es obligatorio y no puede exceder 80 caracteres.");

            if (!EsTextoObligatorio(provincia, maxLen: 80))
                throw new ArgumentOutOfRangeException(nameof(provincia), "Provincia es obligatoria y no puede exceder 80 caracteres.");

            if (!EsTextoObligatorio(distrito, maxLen: 80))
                throw new ArgumentOutOfRangeException(nameof(distrito), "Distrito es obligatorio y no puede exceder 80 caracteres.");

            if (!EsAddressTypeCodeValido(addressTypeCode))
                throw new ArgumentOutOfRangeException(nameof(addressTypeCode), "AddressTypeCode debe constar de 4 dígitos (p.ej., '0000').");

            if (!EsTextoObligatorio(linea, maxLen: 240))
                throw new ArgumentOutOfRangeException(nameof(linea), "La línea de dirección es obligatoria y no puede exceder 240 caracteres.");

            PaisCodigoIso = paisCodigoIso;
            Ubigeo = ubigeo;
            Departamento = departamento;
            Provincia = provincia;
            Distrito = distrito;
            AddressTypeCode = addressTypeCode;
            Linea = linea;
        }

        // ---------------------------- Fábricas ----------------------------

        /// <summary>
        /// Crea la dirección postal conservando exactamente los textos recibidos (sin trims/upper).
        /// </summary>
        public static DireccionPostal From(
            string linea,
            string ubigeo,
            string departamento,
            string provincia,
            string distrito,
            string? paisCodigoIso = "PE",
            string? addressTypeCode = "0000")
            => new(
                paisCodigoIso ?? "PE",
                ubigeo,
                departamento,
                provincia,
                distrito,
                addressTypeCode ?? "0000",
                linea
            );

        /// <summary>
        /// Intenta crear la dirección postal. Devuelve false si alguna validación falla.
        /// </summary>
        public static bool TryFrom(
            string? linea,
            string? ubigeo,
            string? departamento,
            string? provincia,
            string? distrito,
            out DireccionPostal? direccion,
            string? paisCodigoIso = "PE",
            string? addressTypeCode = "0000")
        {
            direccion = null;

            if (!EsPaisValido(paisCodigoIso)) return false;
            if (!EsUbigeoValido(ubigeo)) return false;
            if (!EsTextoObligatorio(departamento, maxLen: 80)) return false;
            if (!EsTextoObligatorio(provincia, maxLen: 80)) return false;
            if (!EsTextoObligatorio(distrito, maxLen: 80)) return false;
            if (!EsAddressTypeCodeValido(addressTypeCode)) return false;
            if (!EsTextoObligatorio(linea, maxLen: 240)) return false;

            direccion = new DireccionPostal(
                paisCodigoIso!,
                ubigeo!,
                departamento!,
                provincia!,
                distrito!,
                addressTypeCode!,
                linea!
            );
            return true;
        }

        // ---------------------------- Igualdad por valor ----------------------------

        public override bool Equals(object? obj)
        {
            if (obj is not DireccionPostal other) return false;

            // Igualdad estricta por todos los componentes (respetando espacios exactamente).
            return string.Equals(PaisCodigoIso, other.PaisCodigoIso, StringComparison.Ordinal)
                && string.Equals(Ubigeo, other.Ubigeo, StringComparison.Ordinal)
                && string.Equals(Departamento, other.Departamento, StringComparison.Ordinal)
                && string.Equals(Provincia, other.Provincia, StringComparison.Ordinal)
                && string.Equals(Distrito, other.Distrito, StringComparison.Ordinal)
                && string.Equals(AddressTypeCode, other.AddressTypeCode, StringComparison.Ordinal)
                && string.Equals(Linea, other.Linea, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(PaisCodigoIso, StringComparer.Ordinal);
            hash.Add(Ubigeo, StringComparer.Ordinal);
            hash.Add(Departamento, StringComparer.Ordinal);
            hash.Add(Provincia, StringComparer.Ordinal);
            hash.Add(Distrito, StringComparer.Ordinal);
            hash.Add(AddressTypeCode, StringComparer.Ordinal);
            hash.Add(Linea, StringComparer.Ordinal);
            return hash.ToHashCode();
        }

        public static bool operator ==(DireccionPostal? left, DireccionPostal? right)
            => left is null ? right is null : left.Equals(right);

        public static bool operator !=(DireccionPostal? left, DireccionPostal? right)
            => !(left == right);

        public override string ToString()
            => $"{Linea}, {Distrito} - {Provincia} - {Departamento} ({Ubigeo}) {PaisCodigoIso}";

        // ---------------------------- Helpers de validación ----------------------------

        private static bool EsPaisValido(string? paisCodigoIso)
            => paisCodigoIso is not null
               && paisCodigoIso.Length == 2
               && paisCodigoIso == "PE"; // restringido a Perú

        private static bool EsUbigeoValido(string? ubigeo)
        {
            if (ubigeo is null || ubigeo.Length != 6) return false;
            for (int i = 0; i < 6; i++)
            {
                char c = ubigeo[i];
                if (c < '0' || c > '9') return false;
            }
            return ubigeo != "000000";
        }

        private static bool EsAddressTypeCodeValido(string? code)
        {
            if (code is null || code.Length != 4) return false;
            for (int i = 0; i < 4; i++)
            {
                char c = code[i];
                if (c < '0' || c > '9') return false;
            }
            return true;
        }

        /// <summary>
        /// Verifica que el texto no sea nulo, no esté vacío "en blanco" (debe tener al menos
        /// un caracter no-espacio) y no exceda el largo máximo. No altera el contenido.
        /// </summary>
        private static bool EsTextoObligatorio(string? s, int maxLen)
        {
            if (s is null) return false;
            if (s.Length == 0 || s.Length > maxLen) return false;

            // Al menos un caracter visible (no solo espacios)
            for (int i = 0; i < s.Length; i++)
            {
                if (!char.IsWhiteSpace(s[i])) return true;
            }
            return false;
        }
    }
}
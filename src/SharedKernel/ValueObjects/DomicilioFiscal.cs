using System;
using System.Diagnostics;

namespace SharedKernel.ValueObjects
{
    /// <summary>
    /// Domicilio fiscal/dirección para UBL (PartyLegalEntity/RegistrationAddress) alineado a SUNAT (Perú).
    /// Mapeo UBL:
    /// - Linea -> cac:AddressLine/cbc:Line                       (Condicional, 3..200, sin \r \n \t)
    /// - Ubigeo -> cbc:ID                                        (Condicional, 6 dígitos != "000000")
    /// - Provincia -> cbc:CityName                               (Condicional, ver regla de trío)
    /// - Departamento -> cbc:CountrySubentity                    (Condicional, ver regla de trío)
    /// - Distrito -> cbc:District                                (Condicional, ver regla de trío)
    /// - AddressTypeCode -> cbc:AddressTypeCode                  (Condicional, 4 dígitos; p.ej. "0000")
    /// - País -> cbc:Country/cbc:IdentificationCode              (Obligatorio a nivel VO)
    ///
    /// Reglas SUNAT:
    /// - Si se informa uno de Departamento/Provincia/Distrito, deben venir los tres (máx. 80 c/u).
    /// - Para PE, Ubigeo si se informa: 6 dígitos y != "000000".
    /// - Linea si se informa: 3..200 y sin CR/LF/TAB.
    /// - Para países != PE, se limpian Ubigeo/Divisiones/AddressTypeCode.
    /// - Si no hay nada que informar (todo vacío), no se debe serializar RegistrationAddress.
    /// </summary>
    [DebuggerDisplay("{ToString()}")]
    public sealed class DomicilioFiscal : IEquatable<DomicilioFiscal>
    {
        public string PaisCodigoIso { get; }        // ISO-3166-1 Alpha-2 (p.ej. "PE")
        public string? Linea { get; }               // cac:AddressLine/cbc:Line (Condicional)
        public string? Ubigeo { get; }              // cbc:ID (Condicional PE)
        public string? Departamento { get; }        // cbc:CountrySubentity (Condicional PE)
        public string? Provincia { get; }           // cbc:CityName (Condicional PE)
        public string? Distrito { get; }            // cbc:District (Condicional PE)
        public string? AddressTypeCode { get; }     // cbc:AddressTypeCode (Condicional, p.ej. "0000" emisor)

        public bool EsPeru => string.Equals(PaisCodigoIso, "PE", StringComparison.OrdinalIgnoreCase);
        public bool TieneLinea => !string.IsNullOrEmpty(Linea);
        public bool TieneUbigeo => !string.IsNullOrEmpty(Ubigeo);
        public bool TieneDivisionTerritorialCompleta =>
            !string.IsNullOrEmpty(Departamento) &&
            !string.IsNullOrEmpty(Provincia) &&
            !string.IsNullOrEmpty(Distrito);

        /// <summary>
        /// Indica si hay contenido suficiente para serializar &lt;RegistrationAddress&gt; en el XML.
        /// </summary>
        public bool DebeSerializarRegistrationAddress =>
            TieneLinea || TieneUbigeo || TieneDivisionTerritorialCompleta || (!string.IsNullOrEmpty(AddressTypeCode) && EsPeru);

        private DomicilioFiscal(
            string paisCodigoIso,
            string? linea,
            string? ubigeo,
            string? departamento,
            string? provincia,
            string? distrito,
            string? addressTypeCode)
        {
            if (!EsPaisValido(paisCodigoIso))
                throw new ArgumentOutOfRangeException(nameof(paisCodigoIso), "El país debe tener código ISO-3166 (2 letras).");

            // Linea: condicional; si viene, 3..200 y sin CR/LF/TAB
            if (!string.IsNullOrEmpty(linea) && !EsLineaValida(linea))
                throw new ArgumentOutOfRangeException(nameof(linea), "La línea debe tener 3..200 caracteres y no contener \\r \\n \\t.");

            if (string.Equals(paisCodigoIso, "PE", StringComparison.OrdinalIgnoreCase))
            {
                // Ubigeo: si viene, validar
                if (!string.IsNullOrEmpty(ubigeo) && !EsUbigeoValido(ubigeo))
                    throw new ArgumentOutOfRangeException(nameof(ubigeo), "Ubigeo inválido: 6 dígitos y distinto de '000000'.");

                // Divisiones: si viene uno, deben venir los tres, cada uno 1..80 (no solo espacios)
                bool algunoDiv = !string.IsNullOrEmpty(departamento) || !string.IsNullOrEmpty(provincia) || !string.IsNullOrEmpty(distrito);
                if (algunoDiv)
                {
                    if (!EsTextoObligatorio(departamento, 80))
                        throw new ArgumentOutOfRangeException(nameof(departamento), "Departamento requerido cuando se informa alguno; máx. 80.");
                    if (!EsTextoObligatorio(provincia, 80))
                        throw new ArgumentOutOfRangeException(nameof(provincia), "Provincia requerida cuando se informa alguno; máx. 80.");
                    if (!EsTextoObligatorio(distrito, 80))
                        throw new ArgumentOutOfRangeException(nameof(distrito), "Distrito requerido cuando se informa alguno; máx. 80.");
                }

                // AddressTypeCode: si viene, 4 dígitos
                if (!string.IsNullOrEmpty(addressTypeCode) && !EsAddressTypeCodeValido(addressTypeCode))
                    throw new ArgumentOutOfRangeException(nameof(addressTypeCode), "AddressTypeCode debe tener 4 dígitos (p.ej. '0000').");
            }
            else
            {
                // País distinto a PE: limpiar campos propios de PE
                ubigeo = null;
                departamento = null;
                provincia = null;
                distrito = null;
                addressTypeCode = null;
            }

            PaisCodigoIso = paisCodigoIso;
            Linea = linea;
            Ubigeo = ubigeo;
            Departamento = departamento;
            Provincia = provincia;
            Distrito = distrito;
            AddressTypeCode = addressTypeCode;
        }

        // Fábricas
        public static DomicilioFiscal FromPeru(
            string? linea = null,
            string? ubigeo = null,
            string? departamento = null,
            string? provincia = null,
            string? distrito = null,
            string? addressTypeCode = null)
            => new("PE", linea, ubigeo, departamento, provincia, distrito, addressTypeCode);

        public static DomicilioFiscal From(
            string paisCodigoIso,
            string? linea = null,
            string? ubigeo = null,
            string? departamento = null,
            string? provincia = null,
            string? distrito = null,
            string? addressTypeCode = null)
            => new(paisCodigoIso, linea, ubigeo, departamento, provincia, distrito, addressTypeCode);

        public static bool TryFromPeru(
            string? linea,
            string? ubigeo,
            string? departamento,
            string? provincia,
            string? distrito,
            string? addressTypeCode,
            out DomicilioFiscal? domicilio)
        {
            domicilio = null;
            try
            {
                domicilio = FromPeru(linea, ubigeo, departamento, provincia, distrito, addressTypeCode);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Igualdad por valor
        public bool Equals(DomicilioFiscal? other)
        {
            if (other is null) return false;
            return string.Equals(PaisCodigoIso, other.PaisCodigoIso, StringComparison.Ordinal)
                && string.Equals(Linea, other.Linea, StringComparison.Ordinal)
                && string.Equals(Ubigeo, other.Ubigeo, StringComparison.Ordinal)
                && string.Equals(Departamento, other.Departamento, StringComparison.Ordinal)
                && string.Equals(Provincia, other.Provincia, StringComparison.Ordinal)
                && string.Equals(Distrito, other.Distrito, StringComparison.Ordinal)
                && string.Equals(AddressTypeCode, other.AddressTypeCode, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj) => Equals(obj as DomicilioFiscal);

        public override int GetHashCode()
        {
            var h = new HashCode();
            h.Add(PaisCodigoIso); h.Add(Linea); h.Add(Ubigeo); h.Add(Departamento);
            h.Add(Provincia); h.Add(Distrito); h.Add(AddressTypeCode);
            return h.ToHashCode();
        }

        public override string ToString()
        {
            string s = string.Empty;
            if (!string.IsNullOrEmpty(Linea)) s = Linea!;
            if (TieneDivisionTerritorialCompleta)
                s = string.IsNullOrEmpty(s) ? $"{Distrito} - {Provincia} - {Departamento}" : $"{s}, {Distrito} - {Provincia} - {Departamento}";
            if (TieneUbigeo)
                s = string.IsNullOrEmpty(s) ? $"({Ubigeo})" : $"{s} ({Ubigeo})";
            return string.IsNullOrEmpty(s) ? PaisCodigoIso : $"{s} {PaisCodigoIso}";
        }

        // Helpers de validación
        private static bool EsPaisValido(string? iso)
        {
            if (iso is null || iso.Length != 2) return false;
            for (int i = 0; i < 2; i++)
            {
                char c = iso[i];
                if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))) return false;
            }
            return true;
        }

        private static bool EsLineaValida(string s)
        {
            if (s.Length < 3 || s.Length > 200) return false;
            bool algunNoEspacio = false;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '\r' || c == '\n' || c == '\t') return false;
                if (!char.IsWhiteSpace(c)) algunNoEspacio = true;
            }
            return algunNoEspacio;
        }

        private static bool EsUbigeoValido(string ubigeo)
        {
            if (ubigeo.Length != 6) return false;
            for (int i = 0; i < 6; i++)
            {
                char c = ubigeo[i];
                if (c < '0' || c > '9') return false;
            }
            return ubigeo != "000000";
        }

        private static bool EsTextoObligatorio(string? s, int maxLen)
        {
            if (s is null || s.Length == 0 || s.Length > maxLen) return false;
            for (int i = 0; i < s.Length; i++) if (!char.IsWhiteSpace(s[i])) return true;
            return false;
        }

        private static bool EsAddressTypeCodeValido(string code)
        {
            if (code.Length != 4) return false;
            for (int i = 0; i < 4; i++)
            {
                char c = code[i];
                if (c < '0' || c > '9') return false;
            }
            return true;
        }
    }
}

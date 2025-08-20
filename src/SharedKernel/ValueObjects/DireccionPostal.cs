using System;
using System.Diagnostics;

namespace SharedKernel.ValueObjects
{
    /// <summary>
    /// Dirección postal conforme a SUNAT/UBL.
    /// Para Perú (PE), Ubigeo/Departamento/Provincia/Distrito son CONDICIONALES.
    /// Línea (AddressLine) se exige siempre; puede ser "-" cuando no se dispone de dirección.
    /// No normaliza espacios ni mayúsculas (conserva tal cual).
    /// La coherencia Ubigeo↔(Dep/Prov/Dist) se valida fuera (catálogo oficial) si aplica.
    /// </summary>
    [DebuggerDisplay("{Linea}, {Distrito} - {Provincia} - {Departamento} ({Ubigeo}) {PaisCodigoIso}")]
    public sealed class DireccionPostal : IEquatable<DireccionPostal>
    {
        public string PaisCodigoIso { get; }
        public string Ubigeo { get; }                // PE: condicional (6 dígitos)
        public string Departamento { get; }          // PE: condicional (junto con Prov/Dist)
        public string Provincia { get; }             // PE: condicional
        public string Distrito { get; }              // PE: condicional
        public string AddressTypeCode { get; }       // PE: condicional (4 dígitos); default "0000"
        public string Linea { get; }                 // Obligatorio siempre (puede ser "-")
        public string? Urbanizacion { get; }         // Opcional
        public string? Referencia { get; }           // Opcional

        public bool EsPeru => string.Equals(PaisCodigoIso, "PE", StringComparison.OrdinalIgnoreCase);
        public bool TieneUbigeo => !string.IsNullOrEmpty(Ubigeo);
        public bool TieneDivisionTerritorialCompleta =>
            !string.IsNullOrEmpty(Departamento) &&
            !string.IsNullOrEmpty(Provincia) &&
            !string.IsNullOrEmpty(Distrito);

        /// <summary>Conveniencia para saber si el usuario dejó la línea como “sin dirección”.</summary>
        public bool EsLineaGuion => Linea == "-";

        // Constructor privado
        private DireccionPostal(
            string paisCodigoIso,
            string linea,
            string ubigeo = "",
            string departamento = "",
            string provincia = "",
            string distrito = "",
            string addressTypeCode = "",
            string? urbanizacion = null,
            string? referencia = null)
        {
            if (!EsPaisValido(paisCodigoIso))
                throw new ArgumentOutOfRangeException(nameof(paisCodigoIso), "El país debe tener código ISO-3166 de 2 letras.");

            if (!EsTextoObligatorio(linea, 240))
                throw new ArgumentOutOfRangeException(nameof(linea), "La línea de dirección es obligatoria y máx. 240 caracteres.");

            if (string.Equals(paisCodigoIso, "PE", StringComparison.OrdinalIgnoreCase))
            {
                // Ubigeo: validar SOLO si viene informado
                if (!string.IsNullOrEmpty(ubigeo) && !EsUbigeoValido(ubigeo))
                    throw new ArgumentOutOfRangeException(nameof(ubigeo), "Ubigeo inválido: 6 dígitos y distinto de '000000'.");

                // Si se informa uno de Dep/Prov/Dist, deben venir los tres
                bool algunoDiv = !string.IsNullOrEmpty(departamento) || !string.IsNullOrEmpty(provincia) || !string.IsNullOrEmpty(distrito);
                if (algunoDiv)
                {
                    if (!EsTextoObligatorio(departamento, 80))
                        throw new ArgumentOutOfRangeException(nameof(departamento), "Departamento máx. 80 caracteres.");
                    if (!EsTextoObligatorio(provincia, 80))
                        throw new ArgumentOutOfRangeException(nameof(provincia), "Provincia máx. 80 caracteres.");
                    if (!EsTextoObligatorio(distrito, 80))
                        throw new ArgumentOutOfRangeException(nameof(distrito), "Distrito máx. 80 caracteres.");
                }

                // AddressTypeCode: default "0000" si no informan; y validar si viene (o tras default)
                if (string.IsNullOrEmpty(addressTypeCode)) addressTypeCode = "0000";
                if (!EsAddressTypeCodeValido(addressTypeCode))
                    throw new ArgumentOutOfRangeException(nameof(addressTypeCode), "AddressTypeCode debe tener 4 dígitos (p.ej. '0000').");
            }
            else
            {
                // Para países distintos a PE, los campos peruanos deben quedar vacíos
                ubigeo = string.Empty;
                departamento = string.Empty;
                provincia = string.Empty;
                distrito = string.Empty;
                addressTypeCode = string.Empty;
            }

            if (!EsTextoOpcional(urbanizacion, 80))
                throw new ArgumentOutOfRangeException(nameof(urbanizacion), "Urbanización supera el máximo permitido (80).");
            if (!EsTextoOpcional(referencia, 120))
                throw new ArgumentOutOfRangeException(nameof(referencia), "Referencia supera el máximo permitido (120).");

            PaisCodigoIso = paisCodigoIso;
            Ubigeo = ubigeo ?? string.Empty;
            Departamento = departamento ?? string.Empty;
            Provincia = provincia ?? string.Empty;
            Distrito = distrito ?? string.Empty;
            AddressTypeCode = addressTypeCode ?? string.Empty;
            Linea = linea;
            Urbanizacion = urbanizacion;
            Referencia = referencia;
        }

        // Fábricas
        public static DireccionPostal FromPeru(
            string linea,
            string? ubigeo = null,
            string? departamento = null,
            string? provincia = null,
            string? distrito = null,
            string? addressTypeCode = "0000",
            string? urbanizacion = null,
            string? referencia = null)
            => new("PE", linea, ubigeo ?? "", departamento ?? "", provincia ?? "", distrito ?? "", addressTypeCode ?? "", urbanizacion, referencia);

        public static DireccionPostal From(
            string paisCodigoIso,
            string linea,
            string? ubigeo = null,
            string? departamento = null,
            string? provincia = null,
            string? distrito = null,
            string? addressTypeCode = null,
            string? urbanizacion = null,
            string? referencia = null)
            => new(paisCodigoIso, linea, ubigeo ?? "", departamento ?? "", provincia ?? "", distrito ?? "", addressTypeCode ?? "", urbanizacion, referencia);

        public static bool TryFromPeru(
            string? linea,
            string? ubigeo,
            string? departamento,
            string? provincia,
            string? distrito,
            out DireccionPostal? direccion,
            string? addressTypeCode = "0000",
            string? urbanizacion = null,
            string? referencia = null)
        {
            direccion = null;
            if (!EsPaisValido("PE")) return false;
            if (!EsTextoObligatorio(linea, 240)) return false;

            // Condicionales
            if (!string.IsNullOrEmpty(ubigeo) && !EsUbigeoValido(ubigeo)) return false;

            bool algunoDiv = !string.IsNullOrEmpty(departamento) || !string.IsNullOrEmpty(provincia) || !string.IsNullOrEmpty(distrito);
            if (algunoDiv)
            {
                if (!EsTextoObligatorio(departamento, 80)) return false;
                if (!EsTextoObligatorio(provincia, 80)) return false;
                if (!EsTextoObligatorio(distrito, 80)) return false;
            }

            // AddressTypeCode: si es null, usar "0000"; validar
            addressTypeCode ??= "0000";
            if (!EsAddressTypeCodeValido(addressTypeCode)) return false;

            if (!EsTextoOpcional(urbanizacion, 80)) return false;
            if (!EsTextoOpcional(referencia, 120)) return false;

            direccion = new DireccionPostal("PE", linea!, ubigeo ?? "", departamento ?? "", provincia ?? "", distrito ?? "", addressTypeCode, urbanizacion, referencia);
            return true;
        }

        // Igualdad por valor (case-sensitive, preserva texto exacto)
        public bool Equals(DireccionPostal? other)
        {
            if (other is null) return false;
            return PaisCodigoIso == other.PaisCodigoIso
                && Ubigeo == other.Ubigeo
                && Departamento == other.Departamento
                && Provincia == other.Provincia
                && Distrito == other.Distrito
                && AddressTypeCode == other.AddressTypeCode
                && Linea == other.Linea
                && Urbanizacion == other.Urbanizacion
                && Referencia == other.Referencia;
        }

        public override bool Equals(object? obj) => Equals(obj as DireccionPostal);

        public override int GetHashCode()
        {
            var h = new HashCode();
            h.Add(PaisCodigoIso); h.Add(Ubigeo); h.Add(Departamento); h.Add(Provincia);
            h.Add(Distrito); h.Add(AddressTypeCode); h.Add(Linea); h.Add(Urbanizacion); h.Add(Referencia);
            return h.ToHashCode();
        }

        public override string ToString()
        {
            // Arma una representación compacta sin “()” vacíos
            string main = Linea;

            // Bloque de división territorial si hay algo
            string div = null!;
            if (TieneDivisionTerritorialCompleta)
            {
                div = $"{Distrito} - {Provincia} - {Departamento}";
            }
            else
            {
                // agrega solo los que existan
                if (!string.IsNullOrEmpty(Distrito) || !string.IsNullOrEmpty(Provincia) || !string.IsNullOrEmpty(Departamento))
                {
                    var d = Distrito;
                    var p = Provincia;
                    var dep = Departamento;

                    // concatena evitando separadores sobrantes
                    if (!string.IsNullOrEmpty(d)) main += $", {d}";
                    if (!string.IsNullOrEmpty(p)) main += (string.IsNullOrEmpty(d) ? ", " : " - ") + p;
                    if (!string.IsNullOrEmpty(dep))
                    {
                        if (!string.IsNullOrEmpty(d) || !string.IsNullOrEmpty(p))
                            main += " - " + dep;
                        else
                            main += ", " + dep;
                    }
                }
            }

            if (!string.IsNullOrEmpty(div)) main += $", {div}";
            if (TieneUbigeo) main += $" ({Ubigeo})";
            return $"{main} {PaisCodigoIso}";
        }

        // Validaciones
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

        private static bool EsUbigeoValido(string? ubigeo)
        {
            if (ubigeo is null || ubigeo.Length != 6) return false;
            for (int i = 0; i < 6; i++) { var c = ubigeo[i]; if (c < '0' || c > '9') return false; }
            return ubigeo != "000000";
        }

        private static bool EsAddressTypeCodeValido(string? code)
        {
            if (code is null || code.Length != 4) return false;
            for (int i = 0; i < 4; i++) { var c = code[i]; if (c < '0' || c > '9') return false; }
            return true;
        }

        private static bool EsTextoObligatorio(string? s, int maxLen)
        {
            if (s is null || s.Length == 0 || s.Length > maxLen) return false;
            for (int i = 0; i < s.Length; i++) if (!char.IsWhiteSpace(s[i])) return true;
            return false;
        }

        private static bool EsTextoOpcional(string? s, int maxLen)
            => s is null || s.Length <= maxLen;
    }
}

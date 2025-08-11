using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ConfiguracionSistemaBC.Domain.ValueObjects
{
    /// <summary>
    /// VO que representa el <b>Tipo de Comprobante SUNAT</b>.
    /// - Igualdad por valor (basada en Código).
    /// - MVP: Factura (01) y Boleta (03). Fácil de extender (07, 08, 09...).
    /// </summary>
    [DebuggerDisplay("{Codigo} - {NombreCorto}")]
    public sealed class TipoComprobanteCodigo : IEquatable<TipoComprobanteCodigo>
    {
        // ---------------------------- Instancias conocidas (MVP) ----------------------------
        public static readonly TipoComprobanteCodigo Factura = new("01", "FACTURA", "Factura");
        public static readonly TipoComprobanteCodigo Boleta  = new("03", "BOLETA",  "Boleta de venta");

        /// <summary>Enumeración de todas las instancias soportadas actualmente.</summary>
        public static IReadOnlyCollection<TipoComprobanteCodigo> All => _byCode.Values;

        /// <summary>Código SUNAT (p. ej., "01", "03"). Es la representación canónica.</summary>
        public string Codigo { get; }

        /// <summary>Nombre corto estandarizado (mayúsculas, sin tildes) útil para UI compacta.</summary>
        public string NombreCorto { get; }

        /// <summary>Descripción legible para UI/impresión.</summary>
        public string Descripcion { get; }

        /// <summary>
        /// Prefijo convencional de serie por tipo (no es regla legal, pero es práctica común):
        /// Factura → 'F', Boleta → 'B'.
        /// </summary>
        public char SeriePrefijoConvencional =>
            this == Factura ? 'F' :
            this == Boleta  ? 'B' : '?';

        // ---------------------------- Infraestructura estática ----------------------------
        private static readonly Dictionary<string, TipoComprobanteCodigo> _byCode =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["01"] = Factura,
                ["03"] = Boleta
            };

        // Aliases aceptados al parsear (para robustez de entrada humana)
        private static readonly Dictionary<string, string> _aliasesToCode =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["01"]              = "01",
                ["FACTURA"]         = "01",
                ["F"]               = "01",

                ["03"]              = "03",
                ["BOLETA"]          = "03",
                ["B"]               = "03",
                ["BOLETA DE VENTA"] = "03"
            };

        // ---------------------------- CTOR privado (inmutable) ----------------------------
        private TipoComprobanteCodigo(string codigo, string nombreCorto, string descripcion)
        {
            if (string.IsNullOrWhiteSpace(codigo) || codigo.Length != 2 || !char.IsDigit(codigo[0]) || !char.IsDigit(codigo[1]))
                throw new ArgumentException("El código del tipo de comprobante debe tener 2 dígitos (p. ej., \"01\").", nameof(codigo));

            Codigo = codigo;
            NombreCorto = nombreCorto?.Trim().ToUpperInvariant()
                ?? throw new ArgumentNullException(nameof(nombreCorto));
            Descripcion = string.IsNullOrWhiteSpace(descripcion) ? NombreCorto : descripcion.Trim();
        }

        // ---------------------------- Fábricas / Parseo ----------------------------
        /// <summary>Crea desde un código SUNAT válido (“01”, “03”). Lanza si no está soportado.</summary>
        public static TipoComprobanteCodigo FromCode(string codigoSunat)
        {
            if (string.IsNullOrWhiteSpace(codigoSunat))
                throw new ArgumentNullException(nameof(codigoSunat));

            var key = codigoSunat.Trim();
            if (_byCode.TryGetValue(key, out var known)) return known;

            throw new ArgumentOutOfRangeException(nameof(codigoSunat),
                $"Código de comprobante no soportado: \"{codigoSunat}\". (MVP: 01=Factura, 03=Boleta).");
        }

        /// <summary>Crea desde código o alias (“01”/“FACTURA”/“F”, “03”/“BOLETA”/“B”).</summary>
        public static TipoComprobanteCodigo From(string codigoOAlias)
        {
            if (!TryParse(codigoOAlias, out var result))
                throw new ArgumentOutOfRangeException(nameof(codigoOAlias),
                    $"Valor no reconocido: \"{codigoOAlias}\". Use 01(Factura) o 03(Boleta).");
            return result!;
        }

        /// <summary>Intenta parsear desde código o alias. False si no es reconocido.</summary>
        public static bool TryParse(string? codigoOAlias, out TipoComprobanteCodigo? result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(codigoOAlias)) return false;

            var key = codigoOAlias.Trim().ToUpperInvariant();

            // Normaliza alias → código canónico
            if (_aliasesToCode.TryGetValue(key, out var canonicalCode))
            {
                result = _byCode[canonicalCode];
                return true;
            }

            return false;
        }

        // ---------------------------- Helpers de dominio ----------------------------
        public bool EsFactura => ReferenceEquals(this, Factura);
        public bool EsBoleta  => ReferenceEquals(this, Boleta);

        /// <summary>
        /// Verifica si el código de serie respeta la convención de prefijo (F/B) para este tipo.
        /// Útil para advertir configuraciones atípicas.
        /// </summary>
        public bool SerieSigueConvencion(string? serieCodigo)
        {
            if (string.IsNullOrWhiteSpace(serieCodigo)) return false;
            var s = serieCodigo.Trim().ToUpperInvariant();
            return s.Length >= 1 && s[0] == SeriePrefijoConvencional;
        }

        // ---------------------------- Igualdad por valor (basada en Código) ----------------------------
        public bool Equals(TipoComprobanteCodigo? other)
            => other is not null && string.Equals(Codigo, other.Codigo, StringComparison.Ordinal);

        public override bool Equals(object? obj)
            => obj is TipoComprobanteCodigo other && Equals(other);

        public override int GetHashCode() => Codigo.GetHashCode(StringComparison.Ordinal);

        public static bool operator ==(TipoComprobanteCodigo? left, TipoComprobanteCodigo? right) => Equals(left, right);
        public static bool operator !=(TipoComprobanteCodigo? left, TipoComprobanteCodigo? right) => !Equals(left, right);

        public override string ToString() => Codigo;

        /// <summary>Conversión implícita a string → devuelve el código SUNAT (p. ej., "01").</summary>
        public static implicit operator string(TipoComprobanteCodigo value) => value.Codigo;

        /// <summary>Conversión explícita desde string (código o alias). Lanza si no es válido.</summary>
        public static explicit operator TipoComprobanteCodigo(string value) => From(value);
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ConfiguracionSistemaBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object para el <b>Tipo de Operación</b> (Catálogo 51 – SUNAT).
    /// Ejemplos:
    /// - 0101 = Venta interna  (DEFAULT)
    /// - 0112 = Venta interna – Sustenta Gastos Deducibles PN
    /// - 0200 = Exportación de bienes
    /// - 1001 = Operación sujeta a detracción
    ///
    /// Este VO solo modela el código/semántica. El bloqueo de edición de la serie
    /// cuando ya está en uso se aplica en la ENTIDAD/AGGREGATE que gobierna la serie.
    /// </summary>
    [DebuggerDisplay("{Codigo} - {Nombre}")]
    public sealed class TipoOperacion
    {
        // -------------------- Instancias conocidas (subset útil y normativo) --------------------

        /// <summary>0101 – Venta interna (valor por defecto en configuración).</summary>
        public static readonly TipoOperacion VentaInterna = new("0101", "VENTA INTERNA");

        /// <summary>0112 – Venta interna – Sustenta Gastos Deducibles PN.</summary>
        public static readonly TipoOperacion VentaInternaGastosDeduciblesPN = new("0112", "VENTA INTERNA – SUSTENTA GASTOS DEDUCIBLES PN");

        /// <summary>0113 – Venta interna – NRUS.</summary>
        public static readonly TipoOperacion VentaInternaNRUS = new("0113", "VENTA INTERNA – NRUS");

        /// <summary>0200 – Exportación de bienes.</summary>
        public static readonly TipoOperacion ExportacionBienes = new("0200", "EXPORTACIÓN DE BIENES");

        /// <summary>0401 – Ventas a no domiciliados que no califican como exportación.</summary>
        public static readonly TipoOperacion VentaNoDomiciliadosNoExport = new("0401", "VENTAS A NO DOMICILIADOS QUE NO CALIFICAN COMO EXPORTACIÓN");

        /// <summary>1001 – Operación sujeta a detracción.</summary>
        public static readonly TipoOperacion DetraccionGeneral = new("1001", "OPERACIÓN SUJETA A DETRACCIÓN");

        /// <summary>1004 – Operación sujeta a detracción – Transporte de carga.</summary>
        public static readonly TipoOperacion DetraccionTransporteCarga = new("1004", "OPERACIÓN SUJETA A DETRACCIÓN – TRANSPORTE DE CARGA");

        /// <summary>
        /// Colección de instancias soportadas (ampliable sin romper API).
        /// </summary>
        public static IReadOnlyCollection<TipoOperacion> All => _byCode.Values;

        /// <summary>Conveniencia: devuelve la opción por defecto (Venta Interna – 0101).</summary>
        public static TipoOperacion Default => VentaInterna;

        // -------------------- Estado inmutable --------------------

        /// <summary>Código SUNAT del Catálogo 51 (p.ej., "0101").</summary>
        public string Codigo { get; }

        /// <summary>Nombre/Descripción corta en mayúsculas.</summary>
        public string Nombre { get; }

        private TipoOperacion(string codigo, string nombre)
        {
            // Validación mínima de código: 4 dígitos
            if (string.IsNullOrWhiteSpace(codigo) || codigo.Length != 4 ||
                !char.IsDigit(codigo[0]) || !char.IsDigit(codigo[1]) ||
                !char.IsDigit(codigo[2]) || !char.IsDigit(codigo[3]))
            {
                throw new ArgumentException("El código de Tipo de Operación debe tener 4 dígitos (ej.: \"0101\").", nameof(codigo));
            }

            Codigo = codigo;
            Nombre = (nombre ?? throw new ArgumentNullException(nameof(nombre))).Trim().ToUpperInvariant();
        }

        // -------------------- Infraestructura de búsqueda/parseo --------------------

        private static readonly Dictionary<string, TipoOperacion> _byCode =
            new(StringComparer.Ordinal)
            {
                ["0101"] = VentaInterna,
                ["0112"] = VentaInternaGastosDeduciblesPN,
                ["0113"] = VentaInternaNRUS,
                ["0200"] = ExportacionBienes,
                ["0401"] = VentaNoDomiciliadosNoExport,
                ["1001"] = DetraccionGeneral,
                ["1004"] = DetraccionTransporteCarga
            };

        // Aliases aceptados para entrada humana
        private static readonly Dictionary<string, string> _aliasToCode =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["0101"] = "0101",
                ["VENTA INTERNA"] = "0101",
                ["VENTA"] = "0101",

                ["0112"] = "0112",
                ["VENTA INTERNA GASTOS DEDUCIBLES"] = "0112",
                ["GASTOS DEDUCIBLES"] = "0112",

                ["0113"] = "0113",
                ["NRUS"] = "0113",
                ["VENTA INTERNA NRUS"] = "0113",

                ["0200"] = "0200",
                ["EXPORTACION BIENES"] = "0200",
                ["EXPORTACIÓN DE BIENES"] = "0200",

                ["0401"] = "0401",
                ["NO DOMICILIADOS NO EXPORTACION"] = "0401",
                ["NO DOMICILIADOS"] = "0401",

                ["1001"] = "1001",
                ["DETRACCION"] = "1001",
                ["DETRACCIÓN"] = "1001",
                ["OPERACION SUJETA A DETRACCION"] = "1001",

                ["1004"] = "1004",
                ["DETRACCION TRANSPORTE CARGA"] = "1004",
                ["TRANSPORTE CARGA DETRACCION"] = "1004",
            };

        /// <summary>
        /// Crea desde un código válido conocido del Catálogo 51.
        /// </summary>
        public static TipoOperacion FromCode(string codigoCat51)
        {
            if (string.IsNullOrWhiteSpace(codigoCat51))
                throw new ArgumentNullException(nameof(codigoCat51));

            if (_byCode.TryGetValue(codigoCat51.Trim(), out var known))
                return known;

            throw new ArgumentOutOfRangeException(nameof(codigoCat51),
                $"Tipo de operación no soportado en el sistema: \"{codigoCat51}\".");
        }

        /// <summary>
        /// Crea desde código o alias (permite entradas humanas como “venta interna”, “detracción”).
        /// </summary>
        public static TipoOperacion From(string codigoOAlias)
        {
            if (!TryParse(codigoOAlias, out var result))
                throw new ArgumentOutOfRangeException(nameof(codigoOAlias),
                    $"Valor de tipo de operación no reconocido: \"{codigoOAlias}\".");
            return result!;
        }

        /// <summary>
        /// Intenta parsear desde código o alias. Devuelve false si no es reconocido.
        /// </summary>
        public static bool TryParse(string? codigoOAlias, out TipoOperacion? result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(codigoOAlias)) return false;

            var key = codigoOAlias.Trim();
            if (_byCode.TryGetValue(key, out result)) return true;

            if (_aliasToCode.TryGetValue(key, out var canonical))
            {
                result = _byCode[canonical];
                return true;
            }
            return false;
        }

        // -------------------- Helpers semánticos --------------------

        /// <summary>True si corresponde a una “Venta interna” (códigos que empiezan con 01xx).</summary>
        public bool EsVentaInterna => Codigo.StartsWith("01", StringComparison.Ordinal);

        /// <summary>True si corresponde a una “Operación sujeta a detracción” (1001–1004).</summary>
        public bool EsSujetaADetraccion =>
            Codigo == "1001" || Codigo == "1002" || Codigo == "1003" || Codigo == "1004";

        // -------------------- Igualdad por valor --------------------
        public override bool Equals(object? obj)
            => obj is TipoOperacion other && string.Equals(Codigo, other.Codigo, StringComparison.Ordinal);

        public override int GetHashCode() => Codigo.GetHashCode(StringComparison.Ordinal);

        public static bool operator ==(TipoOperacion? left, TipoOperacion? right)
            => left is null ? right is null : left.Equals(right);

        public static bool operator !=(TipoOperacion? left, TipoOperacion? right)
            => !(left == right);

        public override string ToString() => Codigo;

        /// <summary>Conversión implícita a string (devuelve el código Cat. 51).</summary>
        public static implicit operator string(TipoOperacion value) => value.Codigo;

        /// <summary>Conversión explícita desde string (código o alias).</summary>
        public static explicit operator TipoOperacion(string value) => From(value);
    }
}
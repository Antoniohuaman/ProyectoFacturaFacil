using System;
using System.Diagnostics;

namespace ConfiguracionSistemaBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object para el código de Serie (p. ej., "F001", "B001").
    /// Reglas:
    /// - Formato básico: 4 caracteres => 1 letra (A-Z) + 3 dígitos (0-9).
    /// - Validación por tipo:
    ///   - Factura (01)  → prefijo 'F'
    ///   - Boleta  (03)  → prefijo 'B'
    /// 
    /// La asociación de serie a establecimiento, correlativos y restricciones de
    /// creación/edición/eliminación se gestionan en la entidad/aggregate, no aquí.
    /// </summary>
    [DebuggerDisplay("{Codigo}")]
    public sealed class SerieCodigo
    {
        /// <summary>Código canónico de la serie (siempre 4 chars, en mayúsculas).</summary>
        public string Codigo { get; }

        /// <summary>Prefijo de la serie (primer carácter, A-Z).</summary>
        public char Prefijo => Codigo[0];

        private SerieCodigo(string codigoNormalizado)
        {
            Codigo = codigoNormalizado;
        }

        // -------------------- Fábricas / Parseo --------------------

        /// <summary>
        /// Crea una serie validando <b>solo</b> el formato básico (A999).
        /// No valida el prefijo contra el tipo de comprobante.
        /// </summary>
        public static SerieCodigo From(string raw)
        {
            if (raw is null) throw new ArgumentNullException(nameof(raw));
            var c = raw.Trim().ToUpperInvariant();

            if (!EsFormatoBasicoValido(c))
                throw new ArgumentOutOfRangeException(nameof(raw),
                    "La serie debe tener 4 caracteres: 1 letra (A-Z) seguida de 3 dígitos (0-9). Ej.: F001, B123.");

            return new SerieCodigo(c);
        }

        /// <summary>
        /// Crea la serie y valida que su prefijo sea correcto para el <paramref name="tipo"/>.
        /// Factura(01)→'F', Boleta(03)→'B'.
        /// </summary>
        public static SerieCodigo ForTipo(string raw, TipoComprobanteCodigo tipo)
        {
            if (tipo is null) throw new ArgumentNullException(nameof(tipo));

            var serie = From(raw); // valida formato básico
            ValidarSegunTipo(serie, tipo);
            return serie;
        }

        /// <summary>Intenta crear validando solo formato básico. False si no cumple.</summary>
        public static bool TryFrom(string? raw, out SerieCodigo? serie)
        {
            serie = null;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            var c = raw.Trim().ToUpperInvariant();
            if (!EsFormatoBasicoValido(c)) return false;

            serie = new SerieCodigo(c);
            return true;
        }

        /// <summary>Intenta crear y validar contra el tipo. False si falla cualquier validación.</summary>
        public static bool TryForTipo(string? raw, TipoComprobanteCodigo tipo, out SerieCodigo? serie)
        {
            serie = null;
            if (tipo is null || string.IsNullOrWhiteSpace(raw)) return false;

            var c = raw.Trim().ToUpperInvariant();
            if (!EsFormatoBasicoValido(c)) return false;
            if (!PrefijoValidoParaTipo(c[0], tipo)) return false;

            serie = new SerieCodigo(c);
            return true;
        }

        // -------------------- Validaciones --------------------

        /// <summary>
        /// Verifica el formato básico (A999). No valida prefijo por tipo.
        /// </summary>
        public static bool EsFormatoBasicoValido(string codigoNormalizado)
        {
            if (codigoNormalizado.Length != 4) return false;

            char c0 = codigoNormalizado[0];
            return (c0 >= 'A' && c0 <= 'Z')
                   && EsDigito(codigoNormalizado[1])
                   && EsDigito(codigoNormalizado[2])
                   && EsDigito(codigoNormalizado[3]);
        }

        /// <summary>
        /// True si esta serie es válida para el tipo de comprobante dado.
        /// (Factura→'F', Boleta→'B').
        /// </summary>
        public bool EsValidaPara(TipoComprobanteCodigo tipo)
        {
            if (tipo is null) throw new ArgumentNullException(nameof(tipo));
            return PrefijoValidoParaTipo(Prefijo, tipo);
        }

        /// <summary>
        /// Lanza excepción si la serie no es válida para el tipo.
        /// </summary>
        public static void ValidarSegunTipo(SerieCodigo serie, TipoComprobanteCodigo tipo)
        {
            if (serie is null) throw new ArgumentNullException(nameof(serie));
            if (tipo is null) throw new ArgumentNullException(nameof(tipo));

            if (!PrefijoValidoParaTipo(serie.Prefijo, tipo))
            {
                var esperado = tipo.SeriePrefijoConvencional; // 'F' o 'B'
                throw new ArgumentException(
                    $"La serie \"{serie.Codigo}\" no es válida para el tipo {tipo.Codigo}. " +
                    $"Debe iniciar con '{esperado}'.", nameof(serie));
            }
        }

        private static bool PrefijoValidoParaTipo(char prefijo, TipoComprobanteCodigo tipo)
        {
            if (tipo.EsFactura) return prefijo == 'F';
            if (tipo.EsBoleta)  return prefijo == 'B';

            // Para otros tipos (07, 08, 09, 31) define tu regla cuando los soportes.
            return true;
        }

        private static bool EsDigito(char c) => c >= '0' && c <= '9';

        // -------------------- Igualdad por valor --------------------
        public override bool Equals(object? obj)
            => obj is SerieCodigo other && string.Equals(Codigo, other.Codigo, StringComparison.Ordinal);

        public override int GetHashCode() => Codigo.GetHashCode(StringComparison.Ordinal);

        public static bool operator ==(SerieCodigo? left, SerieCodigo? right)
            => left is null ? right is null : left.Equals(right);

        public static bool operator !=(SerieCodigo? left, SerieCodigo? right) => !(left == right);

        public override string ToString() => Codigo;

        /// <summary>Conversión implícita a string (p. ej., "F001").</summary>
        public static implicit operator string(SerieCodigo value) => value.Codigo;

        /// <summary>Conversión explícita desde string (valida formato básico).</summary>
        public static explicit operator SerieCodigo(string raw) => From(raw);
    }
}
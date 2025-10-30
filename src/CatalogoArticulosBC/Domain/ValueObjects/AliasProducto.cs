#nullable enable
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    /// <summary>
    /// Alias alternativo del producto para mostrarse en documentos (p. ej., facturas/cotizaciones).
    /// Reglas:
    /// - No vacío ni sólo espacios.
    /// - Longitud 1..120 (ajustable).
    /// - Normaliza: trim y colapsa espacios internos múltiples a uno.
    /// - Igualdad case-insensitive sobre el valor normalizado.
    /// </summary>
    public sealed class AliasProducto : IEquatable<AliasProducto>
    {
        private const int MaxLen = 120;
        public string Valor { get; }

        private AliasProducto(string valorNormalizado)
        {
            Valor = valorNormalizado;
        }

        public static AliasProducto Desde(string valor)
        {
            if (valor is null) throw new ArgumentNullException(nameof(valor));

            // trim extremos
            var trimmed = valor.Trim();

            // colapsar espacios internos (uno o más -> un espacio)
            var collapsed = Regex.Replace(trimmed, @"\s+", " ");

            if (collapsed.Length == 0)
                throw new ArgumentException("El alias no puede estar vacío ni ser sólo espacios.", nameof(valor));

            if (collapsed.Length > MaxLen)
                throw new ArgumentOutOfRangeException(nameof(valor), $"El alias no puede exceder {MaxLen} caracteres.");

            // (Opcional) podría prohibirse saltos de línea u otros controles:
            if (collapsed.Contains('\n') || collapsed.Contains('\r'))
                throw new ArgumentException("El alias no puede contener saltos de línea.", nameof(valor));

            return new AliasProducto(collapsed);
        }

        public override string ToString() => Valor;

        #region Equality (case-insensitive sobre el valor normalizado)
        public bool Equals(AliasProducto? other)
            => other is not null && string.Equals(Valor, other.Valor, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object? obj) => Equals(obj as AliasProducto);

        public override int GetHashCode()
            => StringComparer.OrdinalIgnoreCase.GetHashCode(Valor);
        #endregion
    }
}

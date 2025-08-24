using System;
using System.Linq;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    /// <summary>
    /// Código de barras estándar GS1 (GTIN-8/12/13/14 o UPC-E impreso con 8 dígitos).
    /// Opcional para el artículo.
    /// </summary>
    public sealed class CodigoBarras : IEquatable<CodigoBarras>
    {
        /// <summary>
        /// Valor normalizado (solo dígitos), o null si no aplica.
        /// </summary>
        public string? Valor { get; }

        public CodigoBarras(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                Valor = null; // opcional
                return;
            }

            // Normalización ligera: quitamos espacios y guiones comunes en entrada manual
            var normalized = valor.Trim()
                                  .Replace(" ", string.Empty)
                                  .Replace("-", string.Empty);

            if (normalized.Length is not (8 or 12 or 13 or 14))
                throw new ArgumentException("La longitud debe ser 8, 12, 13 o 14 dígitos.", nameof(valor));

            if (!normalized.All(char.IsDigit))
                throw new ArgumentException("El código debe contener solo dígitos (0-9).", nameof(valor));

            if (!TieneCheckDigitValido(normalized))
                throw new ArgumentException("Dígito verificador inválido.", nameof(valor));

            Valor = normalized;
        }

        public override string? ToString() => Valor;

        public override bool Equals(object? obj) => Equals(obj as CodigoBarras);

        public bool Equals(CodigoBarras? other) =>
            other is not null && string.Equals(Valor, other.Valor, StringComparison.Ordinal);

        public override int GetHashCode() => Valor is null ? 0 : Valor.GetHashCode(StringComparison.Ordinal);

        private static bool TieneCheckDigitValido(string digits)
        {
            // Algoritmo GS1 (mod 10). Último dígito es el check.
            var sinCheck = digits[..^1];
            var esperado = CalcularCheckDigit(sinCheck);
            return digits[^1] == esperado;
        }

        private static char CalcularCheckDigit(string sinCheck)
        {
            int suma = 0;
            bool triple = true; // empezando desde la derecha
            for (int i = sinCheck.Length - 1; i >= 0; i--)
            {
                int d = sinCheck[i] - '0';
                suma += triple ? d * 3 : d;
                triple = !triple;
            }
            int mod = suma % 10;
            int check = (10 - mod) % 10;
            return (char)('0' + check);
        }
    }
}

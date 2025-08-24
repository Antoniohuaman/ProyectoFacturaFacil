using System;
using System.Globalization;
using System.Linq;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    /// <summary>
    /// Código de producto según listado SUNAT (opcional).
    /// <para>Puede ser null/empty si no se especifica.</para>
    /// </summary>
    public sealed class CodigoSUNAT : IEquatable<CodigoSUNAT>
    {
        /// <summary>
        /// Valor del código SUNAT, si se proporcionó (sólo dígitos, 4–8 caracteres).
        /// </summary>
        public string? Valor { get; }

        /// <summary>
        /// Crea una nueva instancia de <see cref="CodigoSUNAT"/>.
        /// </summary>
        /// <param name="valor">
        /// Código de SUNAT; puede ser null o whitespace para indicar que no aplica.
        /// Si se proporciona, debe contener sólo dígitos y tener longitud entre 4 y 8.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Si <paramref name="valor"/> no es null/whitespace y no cumple el patrón.
        /// </exception>
        public CodigoSUNAT(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                Valor = null;
                return;
            }

            var trimmed = valor.Trim();

            if (trimmed.Length != 8)
            {
                throw new ArgumentException(
                    "El Código SUNAT debe tener exactamente 8 dígitos.",
                    nameof(valor));
            }

            if (!trimmed.All(char.IsDigit))
            {
                throw new ArgumentException(
                    "El Código SUNAT debe contener sólo dígitos.",
                    nameof(valor));
            }

            Valor = trimmed;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => Equals(obj as CodigoSUNAT);

        /// <inheritdoc/>
        public bool Equals(CodigoSUNAT? other) =>
            other != null && string.Equals(Valor, other.Valor, StringComparison.Ordinal);

        /// <inheritdoc/>
        public override int GetHashCode() =>
            Valor is null ? 0 : Valor.GetHashCode(StringComparison.Ordinal);

        /// <inheritdoc/>
        public override string ToString() =>
            Valor ?? string.Empty;
    }
}

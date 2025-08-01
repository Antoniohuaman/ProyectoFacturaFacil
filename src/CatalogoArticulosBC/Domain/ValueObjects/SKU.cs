using System;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object que representa el SKU (Stock Keeping Unit) de un producto o servicio.
    /// <para>Campo <c>obligatorio</c>: no puede estar vacío, debe tener entre 1 y 50 caracteres
    /// alfanuméricos, guiones o guiones bajos.</para>
    /// </summary>
    public sealed class SKU : IEquatable<SKU>
    {
        /// <summary>
        /// Cadena que identifica el SKU, normalizada (Trim + Uppercase).
        /// </summary>
        public string Valor { get; }

        /// <summary>
        /// Crea una nueva instancia de <see cref="SKU"/>.
        /// </summary>
        /// <param name="valor">
        /// El valor del SKU. Obligatorio, 1–50 caracteres, sólo letras, dígitos, '-' o '_'.</param>
        /// <exception cref="ArgumentException">
        /// Si <paramref name="valor"/> es nulo, vacío, supera 50 caracteres, o contiene caracteres inválidos.</exception>
        public SKU(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                throw new ArgumentException("El SKU no puede estar vacío.", nameof(valor));

            var trimmed = valor.Trim();
            if (trimmed.Length > 50)
                throw new ArgumentException("El SKU no puede exceder 50 caracteres.", nameof(valor));

            foreach (var c in trimmed)
            {
                if (!char.IsLetterOrDigit(c) && c != '-' && c != '_')
                    throw new ArgumentException(
                        "El SKU sólo puede contener letras, dígitos, guiones o guiones bajos.",
                        nameof(valor));
            }

            Valor = trimmed.ToUpperInvariant();
        }

        public override bool Equals(object? obj) => Equals(obj as SKU);

        public bool Equals(SKU? other) =>
            other is not null && Valor == other.Valor;

        public override int GetHashCode() =>
            Valor.GetHashCode(StringComparison.InvariantCulture);

        public override string ToString() => Valor;
    }
}
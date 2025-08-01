using System;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object que representa la base imponible de ventas.
    /// <para>Cantidad obligatoria: no puede ser negativa.</para>
    /// </summary>
    public sealed class BaseImponibleVentas : IEquatable<BaseImponibleVentas>
    {
        /// <summary>
        /// Valor monetario de la base imponible (>= 0).
        /// </summary>
        public decimal Valor { get; }

        /// <summary>
        /// Crea una nueva instancia de <see cref="BaseImponibleVentas"/>.
        /// </summary>
        /// <param name="valor">Cantidad de la base imponible, mayor o igual a cero.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Si <paramref name="valor"/> es negativo.
        /// </exception>
        public BaseImponibleVentas(decimal valor)
        {
            if (valor < 0m)
                throw new ArgumentOutOfRangeException(
                    nameof(valor),
                    "La base imponible no puede ser negativa.");

            Valor = valor;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) =>
            Equals(obj as BaseImponibleVentas);

        /// <inheritdoc/>
        public bool Equals(BaseImponibleVentas? other) =>
            other is not null && Valor == other.Valor;

        /// <inheritdoc/>
        public override int GetHashCode() =>
            Valor.GetHashCode();

        /// <summary>
        /// Formato legible con dos decimales.
        /// </summary>
        public override string ToString() =>
            Valor.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
    }
}

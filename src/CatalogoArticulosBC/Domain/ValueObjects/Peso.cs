using System;
using System.Globalization;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    /// <summary>
    /// Representa el peso de un producto en kilogramos.
    /// <para>Cantidad opcional: si no se proporciona, la propiedad en el agregado podrá quedar en null.</para>
    /// </summary>
    public sealed class Peso : IEquatable<Peso>
    {
        /// <summary>
        /// Valor del peso en kilogramos (>= 0).
        /// </summary>
        public decimal Valor { get; }

        /// <summary>
        /// Crea una nueva instancia de <see cref="Peso"/>.
        /// </summary>
        /// <param name="valor">Peso en kilogramos; debe ser mayor o igual a cero.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Si <paramref name="valor"/> es negativo.
        /// </exception>
        public Peso(decimal valor)
        {
            if (valor < 0m)
                throw new ArgumentOutOfRangeException(
                    nameof(valor),
                    "El peso no puede ser negativo.");

            Valor = valor;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => Equals(obj as Peso);

        /// <inheritdoc/>
        public bool Equals(Peso? other) =>
            other is not null && Valor == other.Valor;

        /// <inheritdoc/>
        public override int GetHashCode() =>
            Valor.GetHashCode();

        /// <summary>
        /// Devuelve una cadena con el peso formateado en kilos (dos decimales).
        /// </summary>
        public override string ToString() =>
            Valor.ToString("F2", CultureInfo.InvariantCulture) + " kg";
    }
}

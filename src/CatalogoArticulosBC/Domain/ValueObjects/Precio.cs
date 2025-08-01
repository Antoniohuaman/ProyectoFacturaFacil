using System;
using System.Globalization;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object que representa el precio de un producto, opcional al crear el producto.
    /// <para>Cuando no se proporciona, la propiedad en el agregado puede quedar en null.</para>
    /// </summary>
    public sealed class Precio : IEquatable<Precio>
    {
        /// <summary>
        /// Monto numérico del precio (>= 0), en la moneda indicada.
        /// </summary>
        public decimal Monto { get; }

        /// <summary>
        /// Moneda en la que está expresado el precio.
        /// </summary>
        public Moneda Moneda { get; }

        /// <summary>
        /// Indica si este precio incluye IGV (<c>true</c>) o no (<c>false</c>).
        /// </summary>
        public bool IncluyeIGV { get; }

        /// <summary>
        /// Crea un nuevo <see cref="Precio"/>.
        /// </summary>
        /// <param name="monto">Valor del precio; debe ser mayor o igual a cero.</param>
        /// <param name="moneda">Moneda asociada al precio.</param>
        /// <param name="incluyeIGV">Indica si el precio ya incluye IGV, por defecto <c>true</c>.</param>
        /// <exception cref="ArgumentOutOfRangeException">Si <paramref name="monto"/> es negativo.</exception>
        public Precio(decimal monto, Moneda moneda, bool incluyeIGV = true)
        {
            if (monto < 0m)
                throw new ArgumentOutOfRangeException(nameof(monto), "El precio no puede ser negativo.");

            Monto = monto;
            Moneda = moneda;
            IncluyeIGV = incluyeIGV;
        }

        /// <summary>
        /// Obtiene el valor neto (sin IGV) calculado a partir de <see cref="Monto"/> y <see cref="IncluyeIGV"/>.
        /// </summary>
        public decimal ValorSinIGV =>
            IncluyeIGV
                ? Monto / (1 + AfectacionIGV.Gravado18.Tasa)
                : Monto;

        /// <summary>
        /// Obtiene el valor con IGV calculado a partir de <see cref="Monto"/> y <see cref="IncluyeIGV"/>.
        /// </summary>
        public decimal ValorConIGV =>
            IncluyeIGV
                ? Monto
                : Monto * (1 + AfectacionIGV.Gravado18.Tasa);

        /// <inheritdoc/>
        public override bool Equals(object? obj) => Equals(obj as Precio);

        /// <inheritdoc/>
        public bool Equals(Precio? other) =>
            other is not null
            && Monto == other.Monto
            && Moneda == other.Moneda
            && IncluyeIGV == other.IncluyeIGV;

        /// <inheritdoc/>
        public override int GetHashCode() =>
            HashCode.Combine(Monto, Moneda, IncluyeIGV);

        /// <summary>
        /// Devuelve una representación legible: símbolo de moneda y monto formateado.
        /// </summary>
        public override string ToString()
        {
            var simbolo = Moneda.ObtenerSimbolo();
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} {1:F2}{2}",
                simbolo,
                Monto,
                IncluyeIGV ? " (Inc. IGV)" : string.Empty);
        }
    }
}

using System;
using System.Globalization;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object que encapsula la fecha de vencimiento de un producto.
    /// <para>Campo opcional: si no se proporciona, en el agregado puede quedar null.</para>
    /// </summary>
    public sealed class FechaVencimiento : IEquatable<FechaVencimiento>
    {
        /// <summary>
        /// Fecha de vencimiento, sin componente de hora (solo fecha).
        /// </summary>
        public DateTime Valor { get; }

        /// <summary>
        /// Crea una nueva fecha de vencimiento.
        /// </summary>
        /// <param name="fecha">
        /// Fecha a establecer como vencimiento. No puede ser anterior al día actual.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Si <paramref name="fecha"/> es anterior a la fecha de hoy.
        /// </exception>
        public FechaVencimiento(DateTime fecha)
        {
            // Normalizamos a la medianoche para comparar solo fechas
            var fechaSolo = fecha.Date;
            var hoy = DateTime.Today;
            if (fechaSolo < hoy)
                throw new ArgumentOutOfRangeException(
                    nameof(fecha),
                    "La fecha de vencimiento no puede ser anterior a hoy.");

            Valor = fechaSolo;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => Equals(obj as FechaVencimiento);

        /// <inheritdoc/>
        public bool Equals(FechaVencimiento? other) =>
            other is not null && Valor == other.Valor;

        /// <inheritdoc/>
        public override int GetHashCode() => Valor.GetHashCode();

        /// <summary>
        /// Representación legible en formato "yyyy-MM-dd".
        /// </summary>
        public override string ToString() => Valor.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}

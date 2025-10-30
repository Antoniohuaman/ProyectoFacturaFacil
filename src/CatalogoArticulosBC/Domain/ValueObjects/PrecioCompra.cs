#nullable enable
using System;
using SharedKernel.ValueObjects; // Moneda
// using SharedKernel.Exceptions; // Si tienes una DomainException propia, descomenta y úsala

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    /// <summary>
    /// Snapshot del precio de compra del producto.
    /// No interviene en impuestos; solo monto y moneda.
    /// Invariante: Monto >= 0.
    /// </summary>
    public sealed class PrecioCompra : IEquatable<PrecioCompra>
    {
        public decimal Monto { get; }
        public Moneda Moneda { get; }

        private PrecioCompra(decimal monto, Moneda moneda)
        {
            // Normalización opcional a 2 decimales (evita dispersión por IEEE/entrada)
            // Ajusta el modo de redondeo si tu política es distinta.
            Monto = decimal.Round(monto, 2, MidpointRounding.AwayFromZero);
            Moneda = moneda ?? throw new ArgumentNullException(nameof(moneda));
        }

        /// <summary>
        /// Fábrica con validaciones. Mantiene la invariante Monto >= 0.
        /// </summary>
        public static PrecioCompra Desde(decimal monto, Moneda moneda)
        {
            if (monto < 0m)
            {
                // Si tienes una excepción de dominio estándar, úsala aquí:
                // throw new DomainException("El precio de compra no puede ser negativo.");
                throw new ArgumentOutOfRangeException(nameof(monto), "El precio de compra no puede ser negativo.");
            }

            return new PrecioCompra(monto, moneda);
        }

        /// <summary>
        /// Crea una instancia a partir de un monto posiblemente nulo; null -> null.
        /// Útil para mapear DTOs opcionales sin ifs repetitivos.
        /// </summary>
        public static PrecioCompra? DesdeNullable(decimal? monto, Moneda moneda)
            => monto.HasValue ? Desde(monto.Value, moneda) : null;

        public override string ToString()
            => $"{Moneda?.Codigo} {Monto:N2}"; // Ej: PEN 12.50

        #region Equality (Value Object)
        public bool Equals(PrecioCompra? other)
            => other is not null && Monto == other.Monto && Moneda.Equals(other.Moneda);

        public override bool Equals(object? obj) => Equals(obj as PrecioCompra);

        public override int GetHashCode() => HashCode.Combine(Monto, Moneda);
        #endregion
    }
}

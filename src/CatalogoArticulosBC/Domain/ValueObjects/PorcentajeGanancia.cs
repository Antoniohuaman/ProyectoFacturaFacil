#nullable enable
using System;
using System.Globalization;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    /// <summary>
    /// Porcentaje de ganancia del producto (0..100 inclusive).
    /// Semántica de dominio: margen comercial configurado por el usuario para el producto.
    /// </summary>
    public sealed class PorcentajeGanancia : IEquatable<PorcentajeGanancia>
    {
        public decimal Valor { get; } // p.ej. 12.34m == 12.34%

        private PorcentajeGanancia(decimal valor)
        {
            // Conserva 2 decimales con redondeo comercial
            Valor = decimal.Round(valor, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Crea el VO desde un porcentaje 0..100 (incluyente).
        /// </summary>
        public static PorcentajeGanancia Desde(decimal porcentaje)
        {
            if (porcentaje < 0m || porcentaje > 100m)
                throw new ArgumentOutOfRangeException(nameof(porcentaje), "El porcentaje de ganancia debe estar entre 0 y 100.");
            return new PorcentajeGanancia(porcentaje);
        }

        /// <summary>
        /// Crea el VO desde una fracción 0..1 (p.ej., 0.2 => 20%).
        /// </summary>
        public static PorcentajeGanancia DesdeFraccion(decimal fraccion)
        {
            if (fraccion < 0m || fraccion > 1m)
                throw new ArgumentOutOfRangeException(nameof(fraccion), "La fracción debe estar entre 0 y 1.");
            return new PorcentajeGanancia(fraccion * 100m);
        }

        /// <summary>
        /// Retorna el valor como fracción 0..1 (p.ej., 20% => 0.2).
        /// </summary>
        public decimal ComoFraccion() => Valor / 100m;

        public override string ToString()
            => $"{Valor.ToString("0.##", CultureInfo.InvariantCulture)}%";

        #region Equality (Value Object)
        public bool Equals(PorcentajeGanancia? other)
            => other is not null && Valor == other.Valor;

        public override bool Equals(object? obj) => Equals(obj as PorcentajeGanancia);

        public override int GetHashCode() => Valor.GetHashCode();
        #endregion
    }
}

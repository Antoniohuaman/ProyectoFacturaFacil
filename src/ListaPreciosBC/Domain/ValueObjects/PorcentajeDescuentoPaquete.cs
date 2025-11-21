using System;

namespace ListaPreciosBC.Domain.ValueObjects
{
    /// <summary>
    /// Descuento del paquete expresado como porcentaje (0..100).
    /// </summary>
    public sealed class PorcentajeDescuentoPaquete : IEquatable<PorcentajeDescuentoPaquete>
    {
        public decimal Valor { get; }

        private PorcentajeDescuentoPaquete(decimal valor)
        {
            Valor = valor;
        }

        public static PorcentajeDescuentoPaquete Crear(decimal valor)
        {
            if (valor < 0m || valor > 100m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(valor),
                    "El porcentaje de descuento debe estar entre 0 y 100.");
            }

            return new PorcentajeDescuentoPaquete(decimal.Round(valor, 2));
        }

        /// <summary>
        /// Calcula el monto de descuento a partir de un valor base.
        /// </summary>
        public decimal CalcularDescuento(decimal montoBase)
        {
            if (montoBase < 0m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(montoBase),
                    "El monto base para el descuento no puede ser negativo.");
            }

            if (Valor == 0m)
            {
                return 0m;
            }

            return decimal.Round(montoBase * Valor / 100m, 2, MidpointRounding.AwayFromZero);
        }

        #region Equality

        public bool Equals(PorcentajeDescuentoPaquete? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Valor == other.Valor;
        }

        public override bool Equals(object? obj) =>
            ReferenceEquals(this, obj) || obj is PorcentajeDescuentoPaquete other && Equals(other);

        public override int GetHashCode() => Valor.GetHashCode();

        public static bool operator ==(PorcentajeDescuentoPaquete? left, PorcentajeDescuentoPaquete? right) =>
            Equals(left, right);

        public static bool operator !=(PorcentajeDescuentoPaquete? left, PorcentajeDescuentoPaquete? right) =>
            !Equals(left, right);

        #endregion

        public override string ToString() => $"{Valor:0.##}%";
    }
}

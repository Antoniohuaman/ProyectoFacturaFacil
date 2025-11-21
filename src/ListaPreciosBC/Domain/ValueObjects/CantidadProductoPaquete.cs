using System;

namespace ListaPreciosBC.Domain.ValueObjects
{
    /// <summary>
    /// Representa la cantidad de un producto dentro de un paquete.
    /// </summary>
    public sealed class CantidadProductoPaquete : IEquatable<CantidadProductoPaquete>
    {
        public int Valor { get; }

        private CantidadProductoPaquete(int valor)
        {
            Valor = valor;
        }

        public static CantidadProductoPaquete Crear(int valor)
        {
            if (valor <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(valor),
                    "La cantidad de un producto en el paquete debe ser mayor que cero.");
            }

            return new CantidadProductoPaquete(valor);
        }

        public CantidadProductoPaquete Incrementar(int delta)
        {
            if (delta < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(delta),
                    "El incremento de cantidad no puede ser negativo.");
            }

            return Crear(Valor + delta);
        }

        public CantidadProductoPaquete Decrementar(int delta)
        {
            if (delta < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(delta),
                    "El decremento de cantidad no puede ser negativo.");
            }

            var nuevaCantidad = Valor - delta;

            if (nuevaCantidad <= 0)
            {
                throw new InvalidOperationException(
                    "La cantidad resultante debe ser mayor que cero.");
            }

            return Crear(nuevaCantidad);
        }

        #region Equality

        public bool Equals(CantidadProductoPaquete? other)
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
            ReferenceEquals(this, obj) || obj is CantidadProductoPaquete other && Equals(other);

        public override int GetHashCode() => Valor.GetHashCode();

        public static bool operator ==(CantidadProductoPaquete? left, CantidadProductoPaquete? right) =>
            Equals(left, right);

        public static bool operator !=(CantidadProductoPaquete? left, CantidadProductoPaquete? right) =>
            !Equals(left, right);

        #endregion

        public override string ToString() => Valor.ToString();
    }
}

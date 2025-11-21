using System;

namespace ListaPreciosBC.Domain.ValueObjects
{
    /// <summary>
    /// Nombre legible de un paquete de productos.
    /// </summary>
    public sealed class NombrePaquete : IEquatable<NombrePaquete>
    {
        private const int LongitudMaxima = 100;

        public string Valor { get; }

        private NombrePaquete(string valor)
        {
            Valor = valor;
        }

        public static NombrePaquete Crear(string valor)
        {
            if (valor is null)
            {
                throw new ArgumentNullException(nameof(valor));
            }

            var normalizado = valor.Trim();

            if (normalizado.Length == 0)
            {
                throw new ArgumentException("El nombre del paquete no puede estar vacío.", nameof(valor));
            }

            if (normalizado.Length > LongitudMaxima)
            {
                throw new ArgumentException(
                    $"El nombre del paquete no puede exceder {LongitudMaxima} caracteres.",
                    nameof(valor));
            }

            return new NombrePaquete(normalizado);
        }

        #region Equality

        public bool Equals(NombrePaquete? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(Valor, other.Valor, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj) =>
            ReferenceEquals(this, obj) || obj is NombrePaquete other && Equals(other);

        public override int GetHashCode() =>
            StringComparer.Ordinal.GetHashCode(Valor);

        public static bool operator ==(NombrePaquete? left, NombrePaquete? right) =>
            Equals(left, right);

        public static bool operator !=(NombrePaquete? left, NombrePaquete? right) =>
            !Equals(left, right);

        #endregion

        public override string ToString() => Valor;
    }
}

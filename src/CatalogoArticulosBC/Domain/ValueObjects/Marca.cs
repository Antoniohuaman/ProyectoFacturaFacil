using System;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    /// <summary>
    /// Marca de un producto (p. ej. "Coca-Cola", "Nike").
    /// Valor object inmutable que encapsula el nombre de la marca.
    /// </summary>
    public sealed class Marca : IEquatable<Marca>
    {
        /// <summary>
        /// Nombre de la marca, normalizado (trim + uppercase).
        /// </summary>
        public string Nombre { get; }

        /// <summary>
        /// Crea una nueva marca con validación de no vacío y longitud máxima.
        /// </summary>
        /// <param name="nombre">
        /// Nombre no nulo ni vacío de la marca (1–100 caracteres).
        /// </param>
        /// <exception cref="ArgumentException">
        /// Si <paramref name="nombre"/> es nulo, vacío o excede 100 caracteres.
        /// </exception>
        public Marca(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException(
                    "El nombre de la marca es obligatorio.",
                    nameof(nombre));

            var valor = nombre.Trim();
            if (valor.Length > 100)
                throw new ArgumentException(
                    "El nombre de la marca no puede exceder 100 caracteres.",
                    nameof(nombre));

            // Normalizar para igualdad por valor (mayúsculas)
            Nombre = valor.ToUpperInvariant();
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => Equals(obj as Marca);

        /// <inheritdoc />
        public bool Equals(Marca? other) =>
            other is not null && string.Equals(
                Nombre,
                other.Nombre,
                StringComparison.InvariantCulture);

        /// <inheritdoc />
        public override int GetHashCode() =>
            Nombre.GetHashCode(StringComparison.InvariantCulture);

        /// <inheritdoc />
        public override string ToString() => Nombre;
    }
}

using System;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    /// <summary>
    /// Categoría de un producto (p. ej. "Gaseosas", "Lácteos", "Deportes").
    /// </summary>
    public sealed class Categoria : IEquatable<Categoria>
    {
        /// <summary>
        /// Nombre de la categoría, normalizado (se almacena en mayúsculas y sin espacios al inicio/fin).
        /// </summary>
        public string Nombre { get; }

        /// <summary>
        /// Crea una nueva categoría con validación de obligatoriedad y longitud máxima.
        /// </summary>
        /// <param name="nombre">
        /// Nombre no vacío de la categoría (1–100 caracteres).
        /// </param>
        /// <exception cref="ArgumentException">
        /// Si <paramref name="nombre"/> es nulo, vacío o excede 100 caracteres.
        /// </exception>
        public Categoria(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException(
                    "El nombre de la categoría es obligatorio.",
                    nameof(nombre));

            var valor = nombre.Trim();
            if (valor.Length > 100)
                throw new ArgumentException(
                    "El nombre de la categoría no puede exceder 100 caracteres.",
                    nameof(nombre));

            // Normalizar para igualdad por valor
            Nombre = valor.ToUpperInvariant();
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) =>
            Equals(obj as Categoria);

        /// <inheritdoc/>
        public bool Equals(Categoria? other) =>
            other is not null
            && Nombre == other.Nombre;

        /// <inheritdoc/>
        public override int GetHashCode() =>
            Nombre.GetHashCode(StringComparison.InvariantCulture);

        /// <inheritdoc/>
        public override string ToString() => Nombre;
    }
}

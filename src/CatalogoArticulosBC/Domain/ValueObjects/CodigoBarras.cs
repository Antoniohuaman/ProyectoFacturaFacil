using System;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    /// <summary>
    /// Código de barras de un producto (opcional).
    /// Valida longitud (8–20) y normaliza el valor (trim).
    /// </summary>
    public sealed class CodigoBarras : IEquatable<CodigoBarras>
    {
        /// <summary>
        /// El valor del código de barras, o null si no se proporcionó.
        /// </summary>
        public string? Valor { get; }

        /// <summary>
        /// Crea un nuevo <see cref="CodigoBarras"/>.
        /// </summary>
        /// <param name="valor">
        /// El valor del código de barras. Puede ser null o vacío para indicar “no aplica”.
        /// Si no es null/espacio, debe tener entre 8 y 20 caracteres (sin espacios al inicio/fin).
        /// </param>
        /// <exception cref="ArgumentException">
        /// Si <paramref name="valor"/> no es null/espacio y su longitud tras trim queda
        /// fuera del rango [8,20].
        /// </exception>
        public CodigoBarras(string? valor)
        {
            if (!string.IsNullOrWhiteSpace(valor))
            {
                var trimmed = valor.Trim();
                if (trimmed.Length < 8 || trimmed.Length > 20)
                    throw new ArgumentException(
                        "El código de barras debe tener entre 8 y 20 caracteres tras eliminar espacios.",
                        nameof(valor));

                Valor = trimmed;
            }
            else
            {
                // Opcional: el usuario no proporcionó código de barras
                Valor = null;
            }
        }

        /// <inheritdoc/>
        public override string? ToString() => Valor;

        /// <inheritdoc/>
        public override bool Equals(object? obj) =>
            Equals(obj as CodigoBarras);

        /// <inheritdoc/>
        public bool Equals(CodigoBarras? other) =>
            other is not null
            && string.Equals(Valor, other.Valor, StringComparison.Ordinal);

        /// <inheritdoc/>
        public override int GetHashCode() =>
            Valor is null
                ? 0
                : Valor.GetHashCode(StringComparison.Ordinal);
    }
}

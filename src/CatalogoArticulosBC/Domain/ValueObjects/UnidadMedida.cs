using System;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    /// <summary>
    /// Representa la unidad de medida de un producto o servicio (p.ej., "Unidad", "Litro", "Bolsa").
    /// Campo <c>obligatorio</c>: el usuario debe seleccionar o ingresar una unidad válida.
    /// </summary>
    public sealed class UnidadMedida : IEquatable<UnidadMedida>
    {
        /// <summary>
        /// Valor de la unidad de medida, sin espacios al inicio o fin, normalizado en mayúsculas.
        /// </summary>
        public string Valor { get; }

        /// <summary>
        /// Crea una nueva <see cref="UnidadMedida"/>, validando obligatoriedad y longitud.
        /// </summary>
        /// <param name="valor">
        /// Nombre de la unidad de medida (1–50 caracteres).
        /// </param>
        /// <exception cref="ArgumentException">
        /// Si <paramref name="valor"/> es nulo, vacío o excede 50 caracteres.
        /// </exception>
        public UnidadMedida(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                throw new ArgumentException(
                    "La unidad de medida es obligatoria.",
                    nameof(valor));

            var trimmed = valor.Trim();
            if (trimmed.Length < 1 || trimmed.Length > 50)
                throw new ArgumentException(
                    "La unidad de medida debe tener entre 1 y 50 caracteres.",
                    nameof(valor));

            // Normalizamos para igualdad por valor
            Valor = trimmed.ToUpperInvariant();
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => Equals(obj as UnidadMedida);

        /// <inheritdoc/>
        public bool Equals(UnidadMedida? other) =>
            other is not null && Valor == other.Valor;

        /// <inheritdoc/>
        public override int GetHashCode() =>
            Valor.GetHashCode(StringComparison.InvariantCulture);

        /// <inheritdoc/>
        public override string ToString() => Valor;
    }
}

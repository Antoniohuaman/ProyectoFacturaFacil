using System;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    /// <summary>
    /// Código de fábrica de un producto (opcional).
    /// Normaliza el valor aplicando Trim y valida que no esté vacío.
    /// </summary>
    public sealed class CodigoFabrica : IEquatable<CodigoFabrica>
    {
        /// <summary>
        /// Valor del código de fábrica o null si no se proporcionó.
        /// </summary>
        public string? Valor { get; }

        /// <summary>
        /// Crea una nueva instancia de <see cref="CodigoFabrica"/>.
        /// </summary>
        /// <param name="valor">
        /// Código de fábrica. Si es null o solo espacios, se considera no aplicable.
        /// De lo contrario, se almacena la versión recortada.
        /// </param>
        public CodigoFabrica(string? valor)
        {
            if (!string.IsNullOrWhiteSpace(valor))
            {
                var trimmed = valor.Trim();
                Valor = string.IsNullOrEmpty(trimmed) ? null : trimmed;
            }
            else
            {
                Valor = null;
            }
        }

        /// <inheritdoc/>
        public override string? ToString() => Valor;

        /// <inheritdoc/>
        public override bool Equals(object? obj) => Equals(obj as CodigoFabrica);

        /// <inheritdoc/>
        public bool Equals(CodigoFabrica? other) =>
            other is not null && string.Equals(Valor, other.Valor, StringComparison.Ordinal);

        /// <inheritdoc/>
        public override int GetHashCode() =>
            Valor is null ? 0 : Valor.GetHashCode(StringComparison.Ordinal);
    }
}

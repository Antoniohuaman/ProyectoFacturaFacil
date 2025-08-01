using System;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    /// <summary>
    /// Código de lote de un producto (opcional).
    /// Normaliza el valor aplicando Trim y valida que no esté vacío.
    /// </summary>
    public sealed class CodigoLote : IEquatable<CodigoLote>
    {
        /// <summary>
        /// Valor del código de lote, o null si no se proporcionó.
        /// </summary>
        public string? Valor { get; }

        /// <summary>
        /// Crea una nueva instancia de <see cref="CodigoLote"/>.
        /// </summary>
        /// <param name="valor">
        /// Código de lote. Si es null o solo espacios, se considera no aplicable.
        /// De lo contrario, se almacena la versión recortada.
        /// </param>
        public CodigoLote(string? valor)
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
        public override bool Equals(object? obj) => Equals(obj as CodigoLote);

        /// <inheritdoc/>
        public bool Equals(CodigoLote? other) =>
            other is not null && string.Equals(Valor, other.Valor, StringComparison.Ordinal);

        /// <inheritdoc/>
        public override int GetHashCode() =>
            Valor is null ? 0 : Valor.GetHashCode(StringComparison.Ordinal);
    }
}

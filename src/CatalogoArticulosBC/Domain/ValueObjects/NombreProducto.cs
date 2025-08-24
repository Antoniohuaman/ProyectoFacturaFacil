using System;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object que representa el nombre de un producto o servicio.
    /// <para>Campo <c>obligatorio</c>: no puede estar vacío y tiene un máximo de 200 caracteres.</para>
    /// <para>Permite caracteres alfanuméricos, espacios, '/', '-' y '_'.</para>
    /// </summary>
    public sealed class NombreProducto : IEquatable<NombreProducto>
    {
        /// <summary>
        /// Valor del nombre del producto, normalizado (trim).
        /// </summary>
        public string Valor { get; }

        /// <summary>
        /// Crea una nueva instancia de <see cref="NombreProducto"/>.
        /// </summary>
        /// <param name="valor">
        /// Cadena con el nombre del producto o servicio.
        /// Obligatorio, 1–200 caracteres, sólo letras, dígitos, espacios, '/', '-' o '_'.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Si <paramref name="valor"/> es nulo, vacío, excede longitud o contiene caracteres inválidos.
        /// </exception>
        public NombreProducto(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                throw new ArgumentException(
                    "El nombre del producto no puede estar vacío.",
                    nameof(valor));

            var trimmed = valor.Trim();
            if (trimmed.Length > 250)
                throw new ArgumentException(
                    "El nombre del producto no puede exceder 250 caracteres.",
                    nameof(valor));

            foreach (var c in trimmed)
            {
                if (!(
                    char.IsLetterOrDigit(c) ||
                    char.IsWhiteSpace(c) ||
                    c == '/' || c == '-' || c == '_'))
                {
                    throw new ArgumentException(
                        "El nombre del producto sólo puede contener letras, dígitos, espacios, '/', '-' o '_'.",
                        nameof(valor));
                }
            }

            Valor = trimmed;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => Equals(obj as NombreProducto);

        /// <inheritdoc/>
        public bool Equals(NombreProducto? other) =>
            other is not null && Valor == other.Valor;

        /// <inheritdoc/>
        public override int GetHashCode() =>
            Valor.GetHashCode(StringComparison.InvariantCulture);

        /// <inheritdoc/>
        public override string ToString() => Valor;
    }
}

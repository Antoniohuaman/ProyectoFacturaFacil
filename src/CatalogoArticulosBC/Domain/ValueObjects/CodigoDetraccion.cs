using System;
using System.Collections.Generic;
using System.Linq;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    /// <summary>
    /// Representa el código de detracción para un producto afecto a detracciones.
    /// <para>Campo opcional: sólo si el producto aplica detracción.</para>
    /// </summary>
    public sealed class CodigoDetraccion : IEquatable<CodigoDetraccion>
    {
        /// <summary>
        /// Código identificador de detracción (p.ej. "030101").
        /// </summary>
        public string Codigo { get; }

        /// <summary>
        /// Descripción de la detracción (p.ej. "Alimentación").
        /// </summary>
        public string Descripcion { get; }

        /// <summary>
        /// Tasa de detracción aplicable (porcentaje en decimal, p.ej. 0.10m = 10%).
        /// </summary>
        public decimal Tasa { get; }

        // Opciones predefinidas según catálogo SUNAT
        public static IReadOnlyCollection<CodigoDetraccion> Opciones { get; } = new[]
        {
            new CodigoDetraccion("030101", "Alimentación",         0.10m),
            new CodigoDetraccion("030102", "Alojamiento",           0.10m),
            new CodigoDetraccion("030103", "Transporte de carga",   0.06m),
            new CodigoDetraccion("030104", "Transporte de pasajeros",0.12m),
            // … añade aquí más códigos según tu catálogo
        };

        private CodigoDetraccion(string codigo, string descripcion, decimal tasa)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                throw new ArgumentException("El código de detracción es obligatorio.", nameof(codigo));
            if (string.IsNullOrWhiteSpace(descripcion))
                throw new ArgumentException("La descripción de detracción es obligatoria.", nameof(descripcion));
            if (tasa < 0m || tasa > 1m)
                throw new ArgumentOutOfRangeException(nameof(tasa), "La tasa de detracción debe estar entre 0 y 1.");

            Codigo      = codigo.Trim();
            Descripcion = descripcion.Trim();
            Tasa        = tasa;
        }

        /// <summary>
        /// Obtiene la instancia correspondiente a partir de un código.
        /// </summary>
        /// <exception cref="ArgumentException">Si el código no existe en <see cref="Opciones"/>.</exception>
        public static CodigoDetraccion FromCodigo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                throw new ArgumentException("El código de detracción es obligatorio.", nameof(codigo));

            var item = Opciones.FirstOrDefault(x => x.Codigo == codigo.Trim());
            if (item == null)
                throw new ArgumentException($"Código de detracción desconocido: '{codigo}'.", nameof(codigo));

            return item;
        }

        public override bool Equals(object? obj) => Equals(obj as CodigoDetraccion);

        public bool Equals(CodigoDetraccion? other) =>
            other is not null && Codigo == other.Codigo;

        public override int GetHashCode() =>
            Codigo.GetHashCode(StringComparison.InvariantCulture);

        public override string ToString() =>
            $"{Descripcion} ({Tasa:P0})";
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    /// <summary>
    /// Representa el tipo de afectación de IGV para un producto.
    /// <para>Campo <c>obligatorio</c>: el usuario debe elegir una de las opciones predefinidas.</para>
    /// </summary>
    public sealed class AfectacionIGV : IEquatable<AfectacionIGV>
    {
        /// <summary>
        /// Código SUNAT de la afectación (p.ej. "10" para Gravado IGV 18%).
        /// </summary>
        public string Codigo { get; }

        /// <summary>
        /// Descripción legible de la afectación.
        /// </summary>
        public string Descripcion { get; }

        /// <summary>
        /// Tasa de IGV aplicable (porcentaje en decimal, p.ej. 0.18m).
        /// </summary>
        public decimal Tasa { get; }

        private AfectacionIGV(string codigo, string descripcion, decimal tasa)
        {
            Codigo = codigo ?? throw new ArgumentNullException(nameof(codigo));
            Descripcion = descripcion ?? throw new ArgumentNullException(nameof(descripcion));
            if (tasa < 0m)
                throw new ArgumentOutOfRangeException(nameof(tasa), "La tasa de IGV no puede ser negativa.");
            Tasa = tasa;
        }

        // Opciones predefinidas de SUNAT
        public static readonly AfectacionIGV Gravado18   = new("10", "Gravado IGV 18%", 0.18m);
        public static readonly AfectacionIGV Gravado10   = new("12", "Gravado IGV 10% (restaurantes)", 0.10m);
        public static readonly AfectacionIGV Gravado0    = new("11", "Gravado IGV 0%", 0m);
        public static readonly AfectacionIGV Inafecto    = new("20", "Inafecto IGV", 0m);
        public static readonly AfectacionIGV Exonerado   = new("30", "Exonerado IGV", 0m);
        public static readonly AfectacionIGV Exportacion = new("40", "Exportación", 0m);

        /// <summary>
        /// Colección de todas las opciones válidas.
        /// </summary>
        public static IReadOnlyCollection<AfectacionIGV> Opciones { get; } = new[]
        {
            Gravado18,
            Gravado10,
            Gravado0,
            Inafecto,
            Exonerado,
            Exportacion
        };

        /// <summary>
        /// Crea una instancia a partir del código SUNAT.
        /// </summary>
        /// <param name="codigo">Código de afectación.</param>
        /// <returns>La instancia correspondiente.</returns>
        /// <exception cref="ArgumentException">Si el código no es válido.</exception>
        public static AfectacionIGV FromCodigo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                throw new ArgumentException("El código de afectación IGV es obligatorio.", nameof(codigo));

            var item = Opciones.FirstOrDefault(x => x.Codigo == codigo);
            if (item == null)
                throw new ArgumentException($"Afectación IGV desconocida: '{codigo}'", nameof(codigo));

            return item;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => Equals(obj as AfectacionIGV);

        /// <inheritdoc/>
        public bool Equals(AfectacionIGV? other) =>
            other is not null && Codigo == other.Codigo;

        /// <inheritdoc/>
        public override int GetHashCode() => Codigo.GetHashCode(StringComparison.InvariantCulture);

        /// <inheritdoc/>
        public override string ToString() => $"{Descripcion} ({Tasa:P0})";
    }
}
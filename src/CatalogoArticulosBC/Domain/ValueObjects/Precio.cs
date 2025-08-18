using System;
using System.Globalization;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    public sealed class Precio : IEquatable<Precio>
    {
    public decimal Monto { get; }
    public Moneda Moneda { get; }
    public bool IncluyeIGV { get; }
    public AfectacionImpuesto AfectacionImpuesto { get; }

        public Precio(
            decimal monto,
            Moneda moneda,
            AfectacionImpuesto afectacionImpuesto,
            bool incluyeIGV = true)
        {
            if (monto < 0m)
                throw new ArgumentOutOfRangeException(nameof(monto), "El precio no puede ser negativo.");

            Monto              = monto;
            Moneda             = moneda;
            AfectacionImpuesto = afectacionImpuesto ?? throw new ArgumentNullException(nameof(afectacionImpuesto));
            IncluyeIGV         = incluyeIGV;
        }

        public decimal ValorSinIGV =>
            IncluyeIGV
                ? Monto / (1 + ObtenerTasa())
                : Monto;

        public decimal ValorConIGV =>
            IncluyeIGV
                ? Monto
                : Monto * (1 + ObtenerTasa());

        private decimal ObtenerTasa()
        {
            // Solo grava impuesto si corresponde
            return AfectacionImpuesto.GravaImpuesto
                ? TasaImpuesto.IGV18.Fraccion // Puedes parametrizar la tasa si lo necesitas
                : 0m;
        }

        public override bool Equals(object? obj) => Equals(obj as Precio);

        public bool Equals(Precio? other) =>
            other is not null
            && Monto == other.Monto
            && Moneda == other.Moneda
            && IncluyeIGV == other.IncluyeIGV
            && AfectacionImpuesto.Equals(other.AfectacionImpuesto);

        public override int GetHashCode() =>
            HashCode.Combine(Monto, Moneda, IncluyeIGV, AfectacionImpuesto);

        public override string ToString()
        {
            var simbolo = Moneda.Simbolo;
            var sufijo  = IncluyeIGV
                ? $" (Inc. {ObtenerTasa():P0})"
                : $" (+{ObtenerTasa():P0})";
            return $"{simbolo} {Monto:F2}{sufijo}";
        }
    }
}

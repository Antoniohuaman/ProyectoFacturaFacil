using System;
using System.Globalization;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    public sealed class PrecioVenta : IEquatable<PrecioVenta>
    {
    public decimal Monto { get; }
    public Moneda Moneda { get; }
    public bool IncluyeIGV { get; }
    public AfectacionImpuesto AfectacionImpuesto { get; }

        public PrecioVenta(
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

        // Métodos conscientes de la tasa real del producto (18%/10%/0%)
        public decimal ValorSinIGV(SharedKernel.ValueObjects.TasaImpuesto tasa)
        {
            var fraccion = AfectacionImpuesto.GravaImpuesto ? tasa.Fraccion : 0m;
            return IncluyeIGV ? Monto / (1 + fraccion) : Monto;
        }

        public decimal ValorConIGV(SharedKernel.ValueObjects.TasaImpuesto tasa)
        {
            var fraccion = AfectacionImpuesto.GravaImpuesto ? tasa.Fraccion : 0m;
            return IncluyeIGV ? Monto : Monto * (1 + fraccion);
        }

        public override bool Equals(object? obj) => Equals(obj as PrecioVenta);

        public bool Equals(PrecioVenta? other) =>
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
            var sufijo  = IncluyeIGV ? " (Inc. IGV)" : " (+IGV)";
            return $"{simbolo} {Monto:F2}{sufijo}";
        }
    }
}

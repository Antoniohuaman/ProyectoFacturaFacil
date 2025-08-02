using System;
using System.Globalization;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    public sealed class Precio : IEquatable<Precio>
    {
        public decimal Monto { get; }
        public Moneda Moneda { get; }
        public bool IncluyeIGV { get; }
        public AfectacionIGV AfectacionIgv { get; }

        public Precio(
            decimal monto,
            Moneda moneda,
            AfectacionIGV afectacionIgv,
            bool incluyeIGV = true)
        {
            if (monto < 0m)
                throw new ArgumentOutOfRangeException(nameof(monto), "El precio no puede ser negativo.");

            Monto         = monto;
            Moneda        = moneda;
            AfectacionIgv = afectacionIgv ?? throw new ArgumentNullException(nameof(afectacionIgv));
            IncluyeIGV    = incluyeIGV;
        }

        public decimal ValorSinIGV =>
            IncluyeIGV
                ? Monto / (1 + AfectacionIgv.Tasa)
                : Monto;

        public decimal ValorConIGV =>
            IncluyeIGV
                ? Monto
                : Monto * (1 + AfectacionIgv.Tasa);

        public override bool Equals(object? obj) => Equals(obj as Precio);

        public bool Equals(Precio? other) =>
            other is not null
            && Monto == other.Monto
            && Moneda == other.Moneda
            && IncluyeIGV == other.IncluyeIGV
            && AfectacionIgv.Equals(other.AfectacionIgv);

        public override int GetHashCode() =>
            HashCode.Combine(Monto, Moneda, IncluyeIGV, AfectacionIgv);

        public override string ToString()
        {
            var simbolo = Moneda.ObtenerSimbolo();
            var sufijo  = IncluyeIGV
                ? $" (Inc. {AfectacionIgv.Tasa:P0})"
                : $" (+{AfectacionIgv.Tasa:P0})";
            return $"{simbolo} {Monto:F2}{sufijo}";
        }
    }
}

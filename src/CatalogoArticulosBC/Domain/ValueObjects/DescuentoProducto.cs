#nullable enable
using System;
using System.Diagnostics;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    /// <summary>
    /// Descuento predeterminado de un producto (Catálogo).
    /// Puede ser por PORCENTAJE (0 < p ≤ 100) o por IMPORTE (monto > 0).
    /// No calcula impuestos; sólo opera sobre el precio base que reciba.
    /// </summary>
    public sealed class DescuentoProducto : IEquatable<DescuentoProducto>
    {
        public enum Modo
        {
            Ninguno    = 0,
            Porcentaje = 1,
            Importe    = 2
        }

        /// <summary>Modo de descuento.</summary>
        public Modo Tipo { get; }

        /// <summary>Porcentaje (0 &lt; p ≤ 100) si Tipo = Porcentaje; null en otro caso.</summary>
        public decimal? Porcentaje { get; }

        /// <summary>Importe fijo (&gt; 0) si Tipo = Importe; null en otro caso.</summary>
        public decimal? Importe { get; }

        private DescuentoProducto(Modo tipo, decimal? porcentaje, decimal? importe)
        {
            Tipo = tipo;
            Porcentaje = porcentaje;
            Importe = importe;

            // Invariantes:
            switch (Tipo)
            {
                case Modo.Ninguno:
                    if (porcentaje is not null || importe is not null)
                        throw new ArgumentException("Ninguno no debe tener valores.");
                    break;

                case Modo.Porcentaje:
                    if (porcentaje is null)
                        throw new ArgumentNullException(nameof(porcentaje));
                    if (porcentaje.Value <= 0m || porcentaje.Value > 100m)
                        throw new ArgumentOutOfRangeException(nameof(porcentaje), "El porcentaje debe ser mayor a 0 y hasta 100.");
                    if (importe is not null)
                        throw new ArgumentException("Porcentaje no debe tener importe.");
                    // normaliza a 2 decimales para persistencia/consistencia
                    Porcentaje = decimal.Round(porcentaje.Value, 2, MidpointRounding.AwayFromZero);
                    break;

                case Modo.Importe:
                    if (importe is null)
                        throw new ArgumentNullException(nameof(importe));
                    if (importe.Value <= 0m)
                        throw new ArgumentOutOfRangeException(nameof(importe), "El importe debe ser mayor a 0.");
                    if (porcentaje is not null)
                        throw new ArgumentException("Importe no debe tener porcentaje.");
                    // normaliza a 2 decimales
                    Importe = decimal.Round(importe.Value, 2, MidpointRounding.AwayFromZero);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(tipo));
            }
        }

        // -------------------------- Fábricas --------------------------

        public static DescuentoProducto Ninguno() => new(Modo.Ninguno, null, null);

        public static DescuentoProducto DesdePorcentaje(decimal porcentaje) =>
            new(Modo.Porcentaje, porcentaje, null);

        public static DescuentoProducto DesdeImporte(decimal importe) =>
            new(Modo.Importe, null, importe);

        public static bool TryDesdePorcentaje(decimal porcentaje, out DescuentoProducto? d)
        {
            try { d = DesdePorcentaje(porcentaje); return true; }
            catch { d = null; return false; }
        }

        public static bool TryDesdeImporte(decimal importe, out DescuentoProducto? d)
        {
            try { d = DesdeImporte(importe); return true; }
            catch { d = null; return false; }
        }

        // -------------------------- Helpers --------------------------

        public bool EsNinguno     => Tipo == Modo.Ninguno;
        public bool EsPorcentaje  => Tipo == Modo.Porcentaje;
        public bool EsImporte     => Tipo == Modo.Importe;

        /// <summary>
        /// Calcula el descuento a aplicar sobre un precio base (mismo signo que base).
        /// Aplica redondeo a 2 decimales (*AwayFromZero*). Nunca devuelve negativo.
        /// </summary>
        public decimal CalcularDescuentoSobre(decimal precioBase)
        {
            if (precioBase <= 0m || EsNinguno) return 0m;

            decimal d = Tipo switch
            {
                Modo.Porcentaje => precioBase * (Porcentaje!.Value / 100m),
                Modo.Importe    => Importe!.Value,
                _               => 0m
            };

            d = decimal.Round(d, 2, MidpointRounding.AwayFromZero);
            if (d > precioBase) d = precioBase; // cap: no puede exceder el precio
            return d;
        }

        /// <summary>
        /// Precio final luego de aplicar el descuento sobre el precio base.
        /// </summary>
        public decimal AplicarSobre(decimal precioBase)
        {
            var d = CalcularDescuentoSobre(precioBase);
            var result = precioBase - d;
            return result < 0m ? 0m : decimal.Round(result, 2, MidpointRounding.AwayFromZero);
        }

        public override string ToString() =>
            Tipo switch
            {
                Modo.Ninguno    => "Sin descuento",
                Modo.Porcentaje => $"{Porcentaje!.Value:0.##} %",
                Modo.Importe    => $"− {Importe!.Value:0.00}",
                _               => "Descuento"
            };

        // -------------------------- Igualdad --------------------------

        public bool Equals(DescuentoProducto? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Tipo == other.Tipo
                && Porcentaje == other.Porcentaje
                && Importe == other.Importe;
        }

        public override bool Equals(object? obj) => Equals(obj as DescuentoProducto);

        public override int GetHashCode()
        {
            var h = new HashCode();
            h.Add(Tipo);
            h.Add(Porcentaje);
            h.Add(Importe);
            return h.ToHashCode();
        }

        public static bool operator ==(DescuentoProducto? a, DescuentoProducto? b) =>
            a is null ? b is null : a.Equals(b);

        public static bool operator !=(DescuentoProducto? a, DescuentoProducto? b) => !(a == b);
    }
}

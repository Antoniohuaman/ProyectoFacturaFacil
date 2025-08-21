using System;
using System.Diagnostics;

namespace ListaPreciosBC.Domain.ValueObjects
{
    /// <summary>
    /// Tramo de precio por volumen. Define un rango incluyente de cantidades y su ValorPrecio.
    /// - MinCantidad ≥ 1
    /// - MaxCantidad null = "en adelante" (abierto)
    /// - Si MaxCantidad existe, MaxCantidad ≥ MinCantidad
    /// </summary>
    [DebuggerDisplay("[{MinCantidad}..{(MaxCantidad is null ? \"∞\" : MaxCantidad.ToString())}] => {Precio}")]
    public sealed class TramoVolumen :
        IEquatable<TramoVolumen>, IComparable<TramoVolumen>
    {
        /// <summary>Cantidad mínima (incluyente). Debe ser ≥ 1.</summary>
        public int MinCantidad { get; }

        /// <summary>Cantidad máxima (incluyente). Null = "en adelante".</summary>
        public int? MaxCantidad { get; }

        /// <summary>Precio aplicable para el rango.</summary>
        public ValorPrecio Precio { get; }

        private TramoVolumen(int minCantidad, int? maxCantidad, ValorPrecio precio)
        {
            if (minCantidad < 1)
                throw new ArgumentOutOfRangeException(nameof(minCantidad), "MinCantidad debe ser ≥ 1.");

            if (maxCantidad.HasValue && maxCantidad.Value < minCantidad)
                throw new ArgumentOutOfRangeException(nameof(maxCantidad), "MaxCantidad no puede ser menor que MinCantidad.");

            Precio = precio ?? throw new ArgumentNullException(nameof(precio));

            MinCantidad = minCantidad;
            MaxCantidad = maxCantidad;
        }

        /// <summary>
        /// Crea un tramo con rango [min..max] (max puede ser null ⇒ abierto).
        /// </summary>
        public static TramoVolumen Crear(int minCantidad, int? maxCantidad, ValorPrecio precio)
            => new(minCantidad, maxCantidad, precio);

        /// <summary>
        /// Crea un tramo cerrado [min..max].
        /// </summary>
        public static TramoVolumen Cerrado(int minCantidad, int maxCantidad, ValorPrecio precio)
            => new(minCantidad, maxCantidad, precio);

        /// <summary>
        /// Crea un tramo abierto [min..∞).
        /// </summary>
        public static TramoVolumen Desde(int minCantidad, ValorPrecio precio)
            => new(minCantidad, null, precio);

        /// <summary>
        /// Crea un tramo unitario [cantidad..cantidad].
        /// </summary>
        public static TramoVolumen Unitario(int cantidad, ValorPrecio precio)
            => new(cantidad, cantidad, precio);

        /// <summary>
        /// Try-crear sin lanzar excepciones.
        /// </summary>
        public static bool TryCrear(int minCantidad, int? maxCantidad, ValorPrecio? precio, out TramoVolumen? tramo)
        {
            tramo = null;
            if (precio is null) return false;
            try { tramo = new TramoVolumen(minCantidad, maxCantidad, precio); return true; }
            catch { return false; }
        }

        /// <summary>
        /// Devuelve true si la cantidad está dentro del rango (incluyente).
        /// </summary>
        public bool ContieneCantidad(int cantidad)
        {
            if (cantidad < 1) return false;
            if (cantidad < MinCantidad) return false;
            if (MaxCantidad is null) return true;
            return cantidad <= MaxCantidad.Value;
        }

        /// <summary>
        /// Devuelve true si este tramo y el otro se solapan (intersección no vacía).
        /// </summary>
        public bool SeSuperponeCon(TramoVolumen otro)
        {
            // Transformamos los "∞" a int.MaxValue para comparar
            var finA = this.MaxCantidad ?? int.MaxValue;
            var finB = otro.MaxCantidad ?? int.MaxValue;

            var inicio = Math.Max(this.MinCantidad, otro.MinCantidad);
            var fin = Math.Min(finA, finB);

            return inicio <= fin; // inclusivo
        }

        /// <summary>
        /// Devuelve true si los tramos son contiguos (el fin de uno es exactamente el día anterior al inicio del otro).
        /// Requiere límites superiores finitos en al menos uno.
        /// </summary>
        public bool EsContiguoCon(TramoVolumen otro)
        {
            if (this.MaxCantidad.HasValue && otro.MinCantidad == this.MaxCantidad.Value + 1) return true;
            if (otro.MaxCantidad.HasValue && this.MinCantidad == otro.MaxCantidad.Value + 1) return true;
            return false;
        }

        /// <summary>
        /// Devuelve una copia con el mismo rango y un nuevo ValorPrecio.
        /// </summary>
        public TramoVolumen ConPrecio(ValorPrecio nuevo)
            => new(MinCantidad, MaxCantidad, nuevo ?? throw new ArgumentNullException(nameof(nuevo)));

        // -------- Igualdad por valor --------
        public bool Equals(TramoVolumen? other)
            => other is not null
               && MinCantidad == other.MinCantidad
               && Nullable.Equals(MaxCantidad, other.MaxCantidad)
               && Precio.Equals(other.Precio);

        public override bool Equals(object? obj) => Equals(obj as TramoVolumen);

        public override int GetHashCode() => HashCode.Combine(MinCantidad, MaxCantidad, Precio);

        /// <summary>
        /// Orden natural: por MinCantidad asc, luego MaxCantidad (null=∞ va al final).
        /// No considera el precio para el orden.
        /// </summary>
        public int CompareTo(TramoVolumen? other)
        {
            if (other is null) return 1;
            var cmp = MinCantidad.CompareTo(other.MinCantidad);
            if (cmp != 0) return cmp;

            var aMax = this.MaxCantidad ?? int.MaxValue;
            var bMax = other.MaxCantidad ?? int.MaxValue;
            return aMax.CompareTo(bMax);
        }

        public static bool operator ==(TramoVolumen? a, TramoVolumen? b)
            => a is null ? b is null : a.Equals(b);

        public static bool operator !=(TramoVolumen? a, TramoVolumen? b) => !(a == b);

        public override string ToString()
            => MaxCantidad is null
                ? $"[{MinCantidad}..∞] => {Precio}"
                : $"[{MinCantidad}..{MaxCantidad}] => {Precio}";
    }
}
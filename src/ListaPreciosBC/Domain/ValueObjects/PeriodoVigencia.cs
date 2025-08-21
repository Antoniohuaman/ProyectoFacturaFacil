using System;
using System.Diagnostics;

namespace ListaPreciosBC.Domain.ValueObjects
{
    /// <summary>
    /// Periodo de vigencia inclusivo en los bordes, a nivel de <b>fecha</b> (se normaliza .Date).
    /// - <see cref="Desde"/> (obligatorio) y <see cref="Hasta"/> (opcional).
    /// - Invariante: si <c>Hasta</c> existe, entonces <c>Hasta &gt;= Desde</c>.
    /// - Inclusivo: un día está vigente si fecha ∈ [Desde..Hasta]; si Hasta es null, entonces fecha ≥ Desde.
    /// 
    /// Casos de uso típicos:
    /// - Promociones o precios temporales (con o sin fecha fin).
    /// - Selección de precio vigente para una fecha dada (p. ej., fecha de emisión).
    /// 
    /// NOTA: Se opera a nivel de fecha (sin horas/zonas) para evitar problemas de TZ.
    /// </summary>
    [DebuggerDisplay("{Desde:yyyy-MM-dd}..{(Hasta is null ? \"∞\" : Hasta.Value.ToString(\"yyyy-MM-dd\"))}")]
    public sealed class PeriodoVigencia :
        IEquatable<PeriodoVigencia>, IComparable<PeriodoVigencia>
    {
        /// <summary>Fecha de inicio (normalizada a .Date).</summary>
        public DateTime Desde { get; }

        /// <summary>Fecha de fin (normalizada a .Date). Null = abierto (en adelante).</summary>
        public DateTime? Hasta { get; }

        private PeriodoVigencia(DateTime desde, DateTime? hasta)
        {
            Desde = desde.Date;
            Hasta = hasta?.Date;

            if (Hasta.HasValue && Hasta.Value < Desde)
                throw new ArgumentOutOfRangeException(nameof(hasta), "Hasta no puede ser anterior a Desde.");
        }

        /// <summary>
        /// Crea un periodo [desde..hasta]. <paramref name="hasta"/> puede ser null (abierto).
        /// Normaliza ambos a .Date.
        /// </summary>
        public static PeriodoVigencia Crear(DateTime desde, DateTime? hasta)
            => new(desde, hasta);

        /// <summary>
        /// Crea un periodo abierto desde una fecha (desde..∞).
        /// </summary>
        public static PeriodoVigencia DesdeFecha(DateTime desde)
            => new(desde, null);

        /// <summary>
        /// Crea un periodo de un solo día [fecha..fecha].
        /// </summary>
        public static PeriodoVigencia SoloDia(DateTime fecha)
            => new(fecha, fecha);

        /// <summary>
        /// Intenta crear sin lanzar excepciones (valida invariante).
        /// </summary>
        public static bool TryCrear(DateTime desde, DateTime? hasta, out PeriodoVigencia? periodo)
        {
            try { periodo = new PeriodoVigencia(desde, hasta); return true; }
            catch { periodo = null; return false; }
        }

        // ------------------- Predicados de estado -------------------

        /// <summary>Devuelve true si la fecha está dentro del periodo (inclusive).</summary>
        public bool EstaVigenteEn(DateTime fecha)
        {
            var f = fecha.Date;
            if (f < Desde) return false;
            if (Hasta is null) return true;
            return f <= Hasta.Value;
        }

        /// <summary>Devuelve true si el periodo ya expiró para la fecha dada.</summary>
        public bool ExpiradoEn(DateTime fecha)
            => Hasta.HasValue && Hasta.Value < fecha.Date;

        /// <summary>Devuelve true si para la fecha dada, el periodo aún no inicia.</summary>
        public bool AunNoVigenteEn(DateTime fecha)
            => fecha.Date < Desde;

        /// <summary>Conveniencia: vigente hoy (DateTime.Today).</summary>
        public bool VigenteHoy => EstaVigenteEn(DateTime.Today);

        // ------------------- Relaciones con otros periodos -------------------

        /// <summary>
        /// Devuelve true si hay intersección no vacía entre dos periodos.
        /// (Inclusivo en los bordes; periodos contiguos como [1..10] y [11..20] NO se consideran solapados).
        /// </summary>
        public bool SeSuperponeCon(PeriodoVigencia otro)
        {
            // A ∩ B ≠ ∅  <=>  max(A.desde, B.desde) ≤ min(A.hasta?, B.hasta?)
            var inicio = MaxFecha(this.Desde, otro.Desde, treatNullAsMax: true);
            var fin = MinFecha(this.Hasta, otro.Hasta, treatNullAsMax: true);
            if (fin is null) // si alguno es abierto y el otro no termina antes del inicio, podrían superponerse
            {
                // Si al menos uno es abierto y el inicio no es posterior al fin del cerrado, hay solape.
                // Caso ambos abiertos: siempre solapan si comparten algún día (inicio comparado).
                if (this.Hasta is null && otro.Hasta is null) return inicio <= MinFecha(this.Hasta, otro.Hasta, treatNullAsMax: true)!;
                // Si solo uno es cerrado:
                var cerradoFin = this.Hasta ?? otro.Hasta; // uno es no nulo
                return inicio <= cerradoFin!.Value;
            }
            return inicio <= fin.Value;
        }

        /// <summary>
        /// Devuelve true si los periodos son contiguos (el fin de uno es exactamente el día anterior al inicio del otro).
        /// </summary>
        public bool EsContiguoCon(PeriodoVigencia otro)
        {
            // [a..b] contiguo a [c..d] si b existe y c == b+1  ||  d existe y a == d+1
            if (this.Hasta.HasValue && otro.Desde == this.Hasta.Value.AddDays(1)) return true;
            if (otro.Hasta.HasValue && this.Desde == otro.Hasta.Value.AddDays(1)) return true;
            return false;
        }

        /// <summary>
        /// Intersección de periodos (si no hay solape, devuelve null).
        /// </summary>
        public PeriodoVigencia? Interseccion(PeriodoVigencia otro)
        {
            var inicio = MaxFecha(this.Desde, otro.Desde);
            var fin = MinFecha(this.Hasta, otro.Hasta, treatNullAsMax: true); // null = ∞
            if (fin is null || inicio <= fin.Value)
                return new PeriodoVigencia(inicio, fin);
            return null;
        }

        /// <summary>
        /// Unión si se superponen o son contiguos; lanza si están separados.
        /// </summary>
        public PeriodoVigencia UnionSiSolapanOContiguos(PeriodoVigencia otro)
        {
            if (!(SeSuperponeCon(otro) || EsContiguoCon(otro)))
                throw new InvalidOperationException("No se pueden unir periodos separados.");

            var inicio = MinFecha(this.Desde, otro.Desde);
            var fin = MaxFecha(this.Hasta, otro.Hasta, treatNullAsMax: true);
            return new PeriodoVigencia(inicio, fin);
        }

        // ------------------- Transformaciones seguras -------------------

        /// <summary>
        /// Devuelve un nuevo periodo con el mismo Desde y un nuevo Hasta (null = abierto).
        /// </summary>
        public PeriodoVigencia ConHasta(DateTime? nuevoHasta)
            => new(Desde, nuevoHasta);

        /// <summary>
        /// Recorta el final a la fecha indicada (debe ser ≥ Desde). Útil para cerrar vigencias.
        /// </summary>
        public PeriodoVigencia RecortarHasta(DateTime nuevaFechaFin)
            => new(Desde, nuevaFechaFin);

        /// <summary>
        /// Extiende el final a la fecha indicada (debe ser ≥ Hasta actual si existe).
        /// </summary>
        public PeriodoVigencia ExtenderHasta(DateTime nuevaFechaFin)
        {
            if (Hasta.HasValue && nuevaFechaFin.Date < Hasta.Value)
                throw new ArgumentOutOfRangeException(nameof(nuevaFechaFin), "No se puede reducir en ExtenderHasta; use RecortarHasta.");
            return new(Desde, nuevaFechaFin);
        }

        // ------------------- Helpers internos de fecha -------------------

        private static DateTime MaxFecha(DateTime a, DateTime b) => (a >= b) ? a : b;

        private static DateTime MinFecha(DateTime a, DateTime b) => (a <= b) ? a : b;

        private static DateTime MinFecha(DateTime a, DateTime? b)
            => b.HasValue ? MinFecha(a, b.Value) : a;

        private static DateTime MaxFecha(DateTime a, DateTime? b, bool treatNullAsMax = false)
        {
            if (!b.HasValue) return treatNullAsMax ? a : b ?? a; // si null=∞, max(a,∞)=∞ => lo resolverá el llamador
            return MaxFecha(a, b.Value);
        }

        private static DateTime? MaxFecha(DateTime? a, DateTime? b, bool treatNullAsMax = false)
        {
            if (!a.HasValue && !b.HasValue) return treatNullAsMax ? (DateTime?)null : null;
            if (!a.HasValue) return treatNullAsMax ? b : null;
            if (!b.HasValue) return treatNullAsMax ? a : null;
            return MaxFecha(a.Value, b.Value);
        }

        private static DateTime? MinFecha(DateTime? a, DateTime? b, bool treatNullAsMax = false)
        {
            if (!a.HasValue && !b.HasValue) return treatNullAsMax ? (DateTime?)null : null;
            if (!a.HasValue) return treatNullAsMax ? b : null;
            if (!b.HasValue) return treatNullAsMax ? a : null;
            return MinFecha(a.Value, b.Value);
        }

        private static DateTime MinFecha(DateTime? a, DateTime? b, bool treatNullAsMax, out bool anyNull)
        {
            anyNull = !a.HasValue || !b.HasValue;
            return MinFecha(a ?? DateTime.MaxValue, b ?? DateTime.MaxValue);
        }

        // ------------------- Igualdad / Orden / ToString -------------------

        public bool Equals(PeriodoVigencia? other)
            => other is not null
               && Desde == other.Desde
               && Nullable.Equals(Hasta, other.Hasta);

        public override bool Equals(object? obj) => Equals(obj as PeriodoVigencia);

        public override int GetHashCode() => HashCode.Combine(Desde, Hasta);

        /// <summary>
        /// Orden natural: primero por Desde ascendente, y luego por Hasta (null como ∞, al final).
        /// </summary>
        public int CompareTo(PeriodoVigencia? other)
        {
            if (other is null) return 1;
            var cmp = DateTime.Compare(Desde, other.Desde);
            if (cmp != 0) return cmp;

            // Null = infinito (va después)
            if (Hasta is null && other.Hasta is null) return 0;
            if (Hasta is null) return 1;
            if (other.Hasta is null) return -1;
            return DateTime.Compare(Hasta.Value, other.Hasta.Value);
        }

        public override string ToString()
            => Hasta is null
               ? $"{Desde:yyyy-MM-dd}..∞"
               : $"{Desde:yyyy-MM-dd}..{Hasta:yyyy-MM-dd}";
    }
}
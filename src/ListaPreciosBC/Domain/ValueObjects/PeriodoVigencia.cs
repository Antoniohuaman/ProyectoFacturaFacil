#nullable enable
using System;
using System.Diagnostics;

namespace ListaPreciosBC.Domain.ValueObjects
{
    /// <summary>
    /// Periodo de vigencia inclusivo en los bordes, operando a nivel de <b>fecha</b> (sin hora/zona).
    /// Invariantes:
    /// - <see cref="Desde"/> es obligatorio (normalizado a .Date).
    /// - <see cref="Hasta"/> es opcional (normalizado a .Date). Si existe, debe cumplir Hasta ≥ Desde.
    /// Semántica:
    /// - Una fecha está vigente si fecha ∈ [Desde..Hasta]; si Hasta es null, entonces fecha ≥ Desde.
    /// </summary>
    [DebuggerDisplay("{Desde:yyyy-MM-dd}..{(Hasta.HasValue ? Hasta.Value.ToString(\"yyyy-MM-dd\") : \"∞\")}")]
    public sealed class PeriodoVigencia :
        IEquatable<PeriodoVigencia>, IComparable<PeriodoVigencia>
    {
        /// <summary>Fecha de inicio (solo fecha).</summary>
        public DateTime Desde { get; }

        /// <summary>Fecha de fin (solo fecha). Null = abierto (en adelante).</summary>
        public DateTime? Hasta { get; }

        private PeriodoVigencia(DateTime desde, DateTime? hasta)
        {
            Desde = desde.Date;
            Hasta = hasta?.Date;

            if (Hasta.HasValue && Hasta.Value < Desde)
                throw new ArgumentOutOfRangeException(nameof(hasta), "Hasta no puede ser anterior a Desde.");
        }

        // -------------------- Fábricas --------------------

        /// <summary>Crea un periodo [desde..hasta]. <paramref name="hasta"/> puede ser null (abierto).</summary>
        public static PeriodoVigencia Crear(DateTime desde, DateTime? hasta) => new(desde, hasta);

        /// <summary>Crea un periodo abierto desde una fecha (desde..∞).</summary>
        public static PeriodoVigencia DesdeFecha(DateTime desde) => new(desde, null);

        /// <summary>Crea un periodo de un solo día [fecha..fecha].</summary>
        public static PeriodoVigencia SoloDia(DateTime fecha) => new(fecha, fecha);

        /// <summary>Intenta crear sin lanzar excepciones.</summary>
        public static bool TryCrear(DateTime desde, DateTime? hasta, out PeriodoVigencia? periodo)
        {
            try { periodo = new PeriodoVigencia(desde, hasta); return true; }
            catch { periodo = null; return false; }
        }

        // -------------------- Predicados de estado --------------------

        /// <summary>Devuelve true si la fecha está dentro del periodo (inclusive).</summary>
        public bool EstaVigenteEn(DateTime fecha)
        {
            var f = fecha.Date;
            if (f < Desde) return false;
            if (Hasta is null) return true;
            return f <= Hasta.Value;
        }

        /// <summary>Conveniencia para <see cref="DateTimeOffset"/> (se evalúa por fecha local).</summary>
        public bool Contiene(DateTimeOffset fecha) => EstaVigenteEn(fecha.Date);

        /// <summary>Conveniencia para <see cref="DateTime"/>.</summary>
        public bool Contiene(DateTime fecha) => EstaVigenteEn(fecha);

        /// <summary>Devuelve true si ya expiró para la fecha dada.</summary>
        public bool ExpiradoEn(DateTime fecha) => Hasta.HasValue && Hasta.Value < fecha.Date;

        /// <summary>Devuelve true si aún no inicia para la fecha dada.</summary>
        public bool AunNoVigenteEn(DateTime fecha) => fecha.Date < Desde;

        /// <summary>Vigente hoy (DateTime.Today).</summary>
        public bool VigenteHoy => EstaVigenteEn(DateTime.Today);

        // -------------------- Relaciones con otros periodos --------------------

        /// <summary>
        /// True si hay intersección no vacía (bordes inclusivos). Null se considera ∞.
        /// Periodos contiguos (p.ej. [1..10] y [11..20]) <b>no</b> se consideran solapados.
        /// </summary>
        public bool SeSuperponeCon(PeriodoVigencia otro)
        {
            var finA = this.Hasta ?? DateTime.MaxValue;
            var finB = otro.Hasta ?? DateTime.MaxValue;
            return this.Desde <= finB && otro.Desde <= finA;
        }

        /// <summary>
        /// True si el fin de uno es exactamente el día anterior al inicio del otro.
        /// </summary>
        public bool EsContiguoCon(PeriodoVigencia otro)
        {
            return (this.Hasta.HasValue && otro.Desde == this.Hasta.Value.AddDays(1))
                || (otro.Hasta.HasValue && this.Desde == otro.Hasta.Value.AddDays(1));
        }

        /// <summary>
        /// Intersección de periodos (si no hay solape, devuelve null). Null se trata como ∞.
        /// </summary>
        public PeriodoVigencia? Interseccion(PeriodoVigencia otro)
        {
            var inicio = Max(this.Desde, otro.Desde);
            var finCalc = Min(this.Hasta ?? DateTime.MaxValue, otro.Hasta ?? DateTime.MaxValue);
            if (finCalc < inicio) return null;

            DateTime? fin = finCalc == DateTime.MaxValue ? null : finCalc;
            return new PeriodoVigencia(inicio, fin);
        }

        /// <summary>
        /// Unión si se superponen o son contiguos; lanza si están separados.
        /// </summary>
        public PeriodoVigencia UnionSiSolapanOContiguos(PeriodoVigencia otro)
        {
            if (!(SeSuperponeCon(otro) || EsContiguoCon(otro)))
                throw new InvalidOperationException("No se pueden unir periodos separados.");

            var inicio = Min(this.Desde, otro.Desde);
            var finCalc = Max(this.Hasta ?? DateTime.MaxValue, otro.Hasta ?? DateTime.MaxValue);
            DateTime? fin = finCalc == DateTime.MaxValue ? null : finCalc;
            return new PeriodoVigencia(inicio, fin);
        }

        // -------------------- Transformaciones seguras --------------------

        /// <summary>Devuelve un nuevo periodo con el mismo Desde y un nuevo Hasta (null = abierto).</summary>
        public PeriodoVigencia ConHasta(DateTime? nuevoHasta) => new(Desde, nuevoHasta);

        /// <summary>Recorta el final a la fecha indicada (debe ser ≥ Desde). Útil para cerrar vigencias.</summary>
        public PeriodoVigencia RecortarHasta(DateTime nuevaFechaFin) => new(Desde, nuevaFechaFin);

        /// <summary>
        /// Extiende el final a la fecha indicada (si el periodo ya tiene fin y la nueva fecha es anterior, lanza).
        /// </summary>
        public PeriodoVigencia ExtenderHasta(DateTime nuevaFechaFin)
        {
            var nf = nuevaFechaFin.Date;
            if (Hasta.HasValue && nf < Hasta.Value)
                throw new ArgumentOutOfRangeException(nameof(nuevaFechaFin),
                    "No se puede reducir en ExtenderHasta; use RecortarHasta.");
            return new PeriodoVigencia(Desde, nf);
        }

        // -------------------- Igualdad / Orden / ToString --------------------

        public bool Equals(PeriodoVigencia? other)
            => other is not null && Desde == other.Desde && Nullable.Equals(Hasta, other.Hasta);

        public override bool Equals(object? obj) => Equals(obj as PeriodoVigencia);

        public override int GetHashCode() => HashCode.Combine(Desde, Hasta);

        /// <summary>Orden natural: primero por Desde ascendente y luego por Hasta (null como ∞, al final).</summary>
        public int CompareTo(PeriodoVigencia? other)
        {
            if (other is null) return 1;

            var cmp = DateTime.Compare(Desde, other.Desde);
            if (cmp != 0) return cmp;

            if (Hasta is null && other.Hasta is null) return 0;
            if (Hasta is null) return 1;               // null = ∞ → va después
            if (other.Hasta is null) return -1;
            return DateTime.Compare(Hasta.Value, other.Hasta.Value);
        }

        public override string ToString()
            => Hasta is null ? $"{Desde:yyyy-MM-dd}..∞" : $"{Desde:yyyy-MM-dd}..{Hasta:yyyy-MM-dd}";

        // -------------------- Helpers --------------------
        private static DateTime Min(DateTime a, DateTime b) => a <= b ? a : b;
        private static DateTime Max(DateTime a, DateTime b) => a >= b ? a : b;

        // En PeriodoVigencia.cs, junto a las fábricas existentes:

public static PeriodoVigencia Crear(DateTimeOffset desde, DateTimeOffset? hasta)
    => new(desde.Date, hasta?.Date);

public static PeriodoVigencia DesdeFecha(DateTimeOffset desde)
    => new(desde.Date, null);

public static PeriodoVigencia SoloDia(DateTimeOffset fecha)
    => new(fecha.Date, fecha.Date);

    }
}

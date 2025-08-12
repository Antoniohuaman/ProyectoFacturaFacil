using System;

namespace IndicadoresNegocioBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object de Periodo temporal para consultas de indicadores.
    /// 
    /// Características:
    ///  - Inmutable y con igualdad por valor.
    ///  - Soporta periodos alineados a una <see cref="Granularidad"/> (DIA, SEMANA, MES, ANIO)
    ///    y periodos personalizados (sin granularidad).
    ///  - Pensado para FILTROS / segmentación en el dashboard; no se edita desde UI libremente.
    ///
    /// Convenciones:
    ///  - Intervalo inclusivo: [Inicio .. FinInclusive] con tipo DateOnly.
    ///  - Para granularidades alineadas:
    ///      * DIA      => FinInclusive = Inicio
    ///      * SEMANA   => FinInclusive = Inicio + 6 días (Semana = 7 días)
    ///      * MES      => FinInclusive = último día del mes
    ///      * ANIO     => FinInclusive = 31/Dic del año
    ///  - Primer día de semana configurable (por defecto Monday) si Granularidad = SEMANA.
    /// </summary>
    public sealed record Periodo
    {
        /// <summary>Fecha de inicio (inclusive).</summary>
        public DateOnly Inicio { get; }

        /// <summary>Fecha de fin (inclusive).</summary>
        public DateOnly FinInclusive { get; }

        /// <summary>
        /// Granularidad del periodo; null indica periodo personalizado (no alineado).
        /// </summary>
        public Granularidad? Granularidad { get; }

        /// <summary>
        /// Primer día de la semana cuando <see cref="Granularidad"/> = SEMANA.
        /// Ignorado para otras granularidades o personalizado.
        /// </summary>
        public DayOfWeek PrimerDiaSemana { get; }

        /// <summary>Fin exclusivo (día siguiente a <see cref="FinInclusive"/>).</summary>
        public DateOnly FinExclusivo => FinInclusive.AddDays(1);

        /// <summary>Cantidad de días en el periodo (inclusive).</summary>
        public int Dias =>
            FinInclusive.DayNumber - Inicio.DayNumber + 1;

        private Periodo(
            DateOnly inicio,
            DateOnly finInclusive,
            Granularidad? granularidad,
            DayOfWeek primerDiaSemana)
        {
            if (finInclusive < inicio)
                throw new ArgumentException("Fin no puede ser anterior al inicio.");

            Inicio = inicio;
            FinInclusive = finInclusive;
            Granularidad = granularidad; // puede ser null (personalizado)
            PrimerDiaSemana = primerDiaSemana;
        }

        // ==================== FÁBRICAS ALINEADAS ====================

        /// <summary>Periodo de 1 día (alineado a DIA).</summary>
        public static Periodo PorDia(DateOnly dia)
        {
            return new Periodo(
                inicio: dia,
                finInclusive: dia,
                granularidad: Granularidad.Dia,
                primerDiaSemana: DayOfWeek.Monday);
        }

        /// <summary>Periodo semanal alineado al primer día de semana indicado (por defecto Monday).</summary>
        public static Periodo PorSemana(DateOnly fecha, DayOfWeek primerDiaSemana = DayOfWeek.Monday)
        {
            var inicio = Granularidad.Semana.ObtenerInicio(fecha, primerDiaSemana);
            var fin = inicio.AddDays(6);
            return new Periodo(inicio, fin, Granularidad.Semana, primerDiaSemana);
        }

        /// <summary>Periodo mensual alineado (primer al último día del mes).</summary>
        public static Periodo PorMes(int anio, int mes)
        {
            var inicio = new DateOnly(anio, mes, 1);
            var fin = inicio.AddMonths(1).AddDays(-1);
            return new Periodo(inicio, fin, Granularidad.Mes, DayOfWeek.Monday);
        }

        /// <summary>Periodo anual alineado (01/Ene al 31/Dic).</summary>
        public static Periodo PorAnio(int anio)
        {
            var inicio = new DateOnly(anio, 1, 1);
            var fin = new DateOnly(anio, 12, 31);
            return new Periodo(inicio, fin, Granularidad.Anio, DayOfWeek.Monday);
        }

        // ==================== FÁBRICA PERSONALIZADA ====================

        /// <summary>
        /// Crea un periodo personalizado (sin granularidad). Útil para filtros libres (rango de fechas).
        /// </summary>
        public static Periodo Personalizado(DateOnly inicio, DateOnly finInclusive)
        {
            if (finInclusive < inicio)
                throw new ArgumentException("Fin no puede ser anterior al inicio.");

            return new Periodo(inicio, finInclusive, granularidad: null, primerDiaSemana: DayOfWeek.Monday);
        }

        // ==================== UTILITARIOS ====================

        /// <summary>Devuelve true si la fecha está dentro del periodo (inclusive).</summary>
        public bool Contiene(DateOnly fecha) => fecha >= Inicio && fecha <= FinInclusive;

        /// <summary>Devuelve true si este periodo se superpone con otro (inclusive).</summary>
        public bool Interseca(Periodo otro) =>
            Inicio <= otro.FinInclusive && FinInclusive >= otro.Inicio;

        /// <summary>Devuelve true si este periodo contiene completamente al otro.</summary>
        public bool Contiene(Periodo otro) =>
            Inicio <= otro.Inicio && FinInclusive >= otro.FinInclusive;

        /// <summary>
        /// Siguiente periodo de la misma granularidad (solo aplica si no es personalizado).
        /// </summary>
        public Periodo Siguiente()
        {
            if (Granularidad is null)
                throw new NotSupportedException("Siguiente() no aplica a periodos personalizados.");

            if (ReferenceEquals(Granularidad, Domain.ValueObjects.Granularidad.Dia))
                return PorDia(Inicio.AddDays(1));

            if (ReferenceEquals(Granularidad, Domain.ValueObjects.Granularidad.Semana))
                return PorSemana(Inicio.AddDays(7), PrimerDiaSemana);

            if (ReferenceEquals(Granularidad, Domain.ValueObjects.Granularidad.Mes))
                return PorMes(Inicio.AddMonths(1).Year, Inicio.AddMonths(1).Month);

            if (ReferenceEquals(Granularidad, Domain.ValueObjects.Granularidad.Anio))
                return PorAnio(Inicio.AddYears(1).Year);

            throw new InvalidOperationException("Granularidad desconocida.");
        }

        /// <summary>
        /// Periodo anterior de la misma granularidad (solo aplica si no es personalizado).
        /// </summary>
        public Periodo Anterior()
        {
            if (Granularidad is null)
                throw new NotSupportedException("Anterior() no aplica a periodos personalizados.");

            if (ReferenceEquals(Granularidad, Domain.ValueObjects.Granularidad.Dia))
                return PorDia(Inicio.AddDays(-1));

            if (ReferenceEquals(Granularidad, Domain.ValueObjects.Granularidad.Semana))
                return PorSemana(Inicio.AddDays(-7), PrimerDiaSemana);

            if (ReferenceEquals(Granularidad, Domain.ValueObjects.Granularidad.Mes))
                return PorMes(Inicio.AddMonths(-1).Year, Inicio.AddMonths(-1).Month);

            if (ReferenceEquals(Granularidad, Domain.ValueObjects.Granularidad.Anio))
                return PorAnio(Inicio.AddYears(-1).Year);

            throw new InvalidOperationException("Granularidad desconocida.");
        }

        /// <summary>
        /// Verifica (y en caso contrario lanza) que el periodo esté alineado a su granularidad.
        /// Periodos personalizados no requieren alineación.
        /// </summary>
        public void AsegurarAlineado()
        {
            if (Granularidad is null) return; // personalizado

            if (ReferenceEquals(Granularidad, Domain.ValueObjects.Granularidad.Dia))
            {
                if (FinInclusive != Inicio)
                    throw new InvalidOperationException("Periodo DIA debe tener Inicio == Fin.");
                return;
            }

            if (ReferenceEquals(Granularidad, Domain.ValueObjects.Granularidad.Semana))
            {
                var esperadoInicio = Domain.ValueObjects.Granularidad.Semana.ObtenerInicio(Inicio, PrimerDiaSemana);
                if (Inicio != esperadoInicio || FinInclusive != Inicio.AddDays(6))
                    throw new InvalidOperationException("Periodo SEMANA no está alineado al inicio/fin esperados.");
                return;
            }

            if (ReferenceEquals(Granularidad, Domain.ValueObjects.Granularidad.Mes))
            {
                if (Inicio.Day != 1 || FinInclusive != Inicio.AddMonths(1).AddDays(-1))
                    throw new InvalidOperationException("Periodo MES no está alineado al primer/último día.");
                return;
            }

            if (ReferenceEquals(Granularidad, Domain.ValueObjects.Granularidad.Anio))
            {
                if (Inicio != new DateOnly(Inicio.Year, 1, 1) ||
                    FinInclusive != new DateOnly(Inicio.Year, 12, 31))
                    throw new InvalidOperationException("Periodo ANIO no está alineado al año completo.");
                return;
            }

            throw new InvalidOperationException("Granularidad desconocida.");
        }

        public override string ToString()
        {
            var gran = Granularidad?.Nombre ?? "PERSONALIZADO";
            return $"{gran} [{Inicio:yyyy-MM-dd}..{FinInclusive:yyyy-MM-dd}]";
        }
    }
}
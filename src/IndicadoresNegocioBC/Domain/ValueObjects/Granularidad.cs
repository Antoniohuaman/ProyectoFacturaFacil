using System;
using System.Collections.Generic;

namespace IndicadoresNegocioBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object de Granularidad temporal para KPIs y periodos.
    /// Estados soportados (smart-enum):
    ///  - DIA
    ///  - SEMANA (inicio configurable; por defecto lunes)
    ///  - MES
    ///  - ANIO
    ///
    /// Métodos utilitarios:
    ///  - ObtenerInicio(fecha, primerDiaSemana): devuelve el inicio del bloque que contiene a 'fecha'.
    ///  - SiguienteInicio(inicio, primerDiaSemana): devuelve el siguiente inicio de bloque.
    ///  - AsegurarAlineado(inicio, primerDiaSemana): valida que 'inicio' esté alineado a la granularidad.
    /// </summary>
    public sealed record Granularidad
    {
        public byte Codigo { get; }
        public string Nombre { get; }

        private Granularidad(byte codigo, string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre es obligatorio.", nameof(nombre));

            Codigo = codigo;
            Nombre = nombre.Trim().ToUpperInvariant();
        }

        // --------- Instancias soportadas (singleton/smart-enum) ---------
        public static readonly Granularidad Dia    = new(1, "DIA");
        public static readonly Granularidad Semana = new(2, "SEMANA");
        public static readonly Granularidad Mes    = new(3, "MES");
        public static readonly Granularidad Anio   = new(4, "ANIO");

        public static IReadOnlyList<Granularidad> Todos { get; } =
            new[] { Dia, Semana, Mes, Anio };

        public override string ToString() => Nombre;

        // ----------------- Fábricas / Parse -----------------
        public static Granularidad DesdeCodigo(byte codigo) => codigo switch
        {
            1 => Dia,
            2 => Semana,
            3 => Mes,
            4 => Anio,
            _ => throw new ArgumentOutOfRangeException(nameof(codigo), "Código de granularidad inválido.")
        };

        public static Granularidad DesdeTexto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                throw new ArgumentException("La granularidad es obligatoria.", nameof(texto));

            var t = texto.Trim().ToUpperInvariant();
            return t switch
            {
                "DIA" or "DIARIO" or "DAY"        => Dia,
                "SEMANA" or "SEMANAL" or "WEEK"   => Semana,
                "MES" or "MENSUAL" or "MONTH"     => Mes,
                "ANIO" or "AÑO" or "ANUAL" or "YEAR" => Anio,
                _ => throw new ArgumentException($"Granularidad desconocida: '{texto}'.", nameof(texto))
            };
        }

        // ----------------- Utilitarios de calendario -----------------

        /// <summary>
        /// Devuelve el inicio del bloque de esta granularidad que contiene a <paramref name="fecha"/>.
        /// Para SEMANA, el inicio es el <paramref name="primerDiaSemana"/> (por defecto Monday).
        /// </summary>
        public DateOnly ObtenerInicio(DateOnly fecha, DayOfWeek primerDiaSemana = DayOfWeek.Monday)
        {
            return this switch
            {
                var g when ReferenceEquals(g, Dia)    => fecha,
                var g when ReferenceEquals(g, Semana) => InicioDeSemana(fecha, primerDiaSemana),
                var g when ReferenceEquals(g, Mes)    => new DateOnly(fecha.Year, fecha.Month, 1),
                var g when ReferenceEquals(g, Anio)   => new DateOnly(fecha.Year, 1, 1),
                _ => throw new InvalidOperationException("Granularidad no reconocida.")
            };
        }

        /// <summary>
        /// Devuelve el siguiente inicio de bloque a partir de un <paramref name="inicio"/> alineado.
        /// Si 'inicio' no está alineado, se alinea primero y luego avanza un bloque.
        /// </summary>
        public DateOnly SiguienteInicio(DateOnly inicio, DayOfWeek primerDiaSemana = DayOfWeek.Monday)
        {
            var aligned = ObtenerInicio(inicio, primerDiaSemana);

            return this switch
            {
                var g when ReferenceEquals(g, Dia)    => aligned.AddDays(1),
                var g when ReferenceEquals(g, Semana) => aligned.AddDays(7),
                var g when ReferenceEquals(g, Mes)    => aligned.AddMonths(1),
                var g when ReferenceEquals(g, Anio)   => aligned.AddYears(1),
                _ => throw new InvalidOperationException("Granularidad no reconocida.")
            };
        }

        /// <summary>
        /// Lanza excepción si <paramref name="inicio"/> no corresponde al inicio exacto del bloque.
        /// Útil para construir periodos con límites correctos (p.ej. snapshots).
        /// </summary>
        public void AsegurarAlineado(DateOnly inicio, DayOfWeek primerDiaSemana = DayOfWeek.Monday)
        {
            var esperado = ObtenerInicio(inicio, primerDiaSemana);
            if (esperado != inicio)
                throw new InvalidOperationException($"La fecha {inicio:yyyy-MM-dd} no está alineada al inicio de {Nombre} (debió ser {esperado:yyyy-MM-dd}).");
        }

        // ----------------- Helpers internos -----------------
        private static DateOnly InicioDeSemana(DateOnly fecha, DayOfWeek primerDiaSemana)
        {
            // Distancia hacia atrás hasta 'primerDiaSemana' (p. ej., Monday).
            int diff = ((7 + ((int)fecha.DayOfWeek - (int)primerDiaSemana)) % 7);
            return fecha.AddDays(-diff);
        }
    }
}
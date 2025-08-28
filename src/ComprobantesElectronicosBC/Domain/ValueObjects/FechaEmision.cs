using System;
using SharedKernel.Exceptions;
using ComprobantesElectronicosBC.Domain.Exceptions;

namespace ComprobantesElectronicosBC.Domain.ValueObjects
{
    /// <summary>
    /// Fecha de emisión de un CPE (UBL: cbc:IssueDate + cbc:IssueTime).
    /// - Siempre es una fecha de calendario (DateOnly) y una hora de captura (TimeOnly).
    /// - No permite fechas futuras (IssueDate ≤ hoy).
    /// - Retroactivo permitido:
    ///     * Factura (tipo "01"): hasta 3 días hacia atrás.
    ///     * Boleta  (tipo "03"): hasta 5 días hacia atrás.
    /// - La validación depende del tipo de comprobante (Catálogo SUNAT 01/03).
    ///
    /// Notas:
    /// - La UI normalmente usa Hoy(tipo) para precargar la fecha = hoy.
    /// - Para pruebas, se puede pasar un "now" fijo a los métodos de fábrica.
    /// </summary>
    public sealed record FechaEmision
    {
        /// <summary>Fecha de calendario que irá a &lt;cbc:IssueDate&gt; (YYYY-MM-DD).</summary>
        public DateOnly Fecha { get; }

        /// <summary>Hora capturada al momento de construir el valor (para &lt;cbc:IssueTime&gt;).</summary>
        public TimeOnly Hora { get; }

        private FechaEmision(DateOnly fecha, TimeOnly hora)
        {
            Fecha = fecha;
            Hora  = hora;
        }

        // ===================== FÁBRICAS PRINCIPALES =====================

        /// <summary>
        /// Crea con la fecha de hoy (pensado para precargar el formulario).
        /// </summary>
        public static FechaEmision Hoy(string tipoComprobante, DateTime? now = null)
        {
            var nowVal = now ?? DateTime.Now;
            var hoy    = DateOnly.FromDateTime(nowVal);
            return Create(hoy, tipoComprobante, nowVal);
        }

        /// <summary>
        /// Crea con una fecha explícita y valida contra las reglas de retroactivo según tipo.
        /// - Lanza ArgumentException si:
        ///   * la fecha es futura, o
        ///   * excede el retroactivo permitido para el tipo dado.
        /// </summary>
        public static FechaEmision Create(DateOnly fecha, string tipoComprobante, DateTime? now = null)
        {
            var tipo  = NormalizeTipo(tipoComprobante);
            var nowDt = now ?? DateTime.Now;
            var hoy   = DateOnly.FromDateTime(nowDt);

            if (fecha > hoy)
                throw new FechaInvalidaException("La fecha de emisión no puede ser futura.");

            // Ventana de retroactivo por normativa
            var ventana = DiasRetroactivoPermitidos(tipo);
            var deltaDias = hoy.DayNumber - fecha.DayNumber; // diferencia absoluta en días

            if (deltaDias > ventana)
                throw new FechaInvalidaException(
                    $"Retroactivo excede {ventana} días para el tipo {DescribeTipo(tipo)}.");

            // Para UBL también incluimos la hora de emisión (capturamos la hora actual)
            var hora = TimeOnly.FromDateTime(nowDt);
            return new FechaEmision(fecha, hora);
        }

        // ===================== QUERIES / HELPERS =====================

        /// <summary>
        /// Devuelve 3 para Factura ("01"), 5 para Boleta ("03").
        /// </summary>
        public static int DiasRetroactivoPermitidos(string tipo)
            => tipo switch
            {
                "01" => 3, // Factura
                "03" => 5, // Boleta
                _    => throw new FechaInvalidaException(
                            "Tipo de comprobante no soportado para FechaEmision. Use \"01\" (Factura) o \"03\" (Boleta).")
            };

        /// <summary>Texto legible para mensajes.</summary>
        private static string DescribeTipo(string tipo)
            => tipo switch { "01" => "Factura (01)", "03" => "Boleta (03)", _ => tipo };

        /// <summary>Normaliza el tipo: recorta espacios y valida que sea "01" o "03".</summary>
        private static string NormalizeTipo(string raw)
        {
            var s = string.IsNullOrWhiteSpace(raw) ? "" : raw.Trim();
            return s switch
            {
                "01" => s,
                "03" => s,
                _    => throw new FechaInvalidaException("Tipo inválido. Use \"01\" (Factura) o \"03\" (Boleta).")
            };
        }

        public override string ToString() => $"{Fecha:yyyy-MM-dd} {Hora:HH\\:mm\\:ss}";
    }
}

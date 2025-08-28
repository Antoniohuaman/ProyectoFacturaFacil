using System;
using SharedKernel.Exceptions;
using ComprobantesElectronicosBC.Domain.Exceptions;

namespace ComprobantesElectronicosBC.Domain.ValueObjects
{
    /// <summary>
    /// VO de fecha de vencimiento (PaymentDueDate en UBL).
    /// - Invariantes:
    ///   * Siempre debe ser >= FechaEmision.
    ///   * En CONTADO: Vencimiento == Emision.
    ///   * En CRÉDITO: Vencimiento = Emision + días (días > 0).
    /// - Notas:
    ///   * Este VO NO valida “>= hoy”. Esa es una regla de UI/Aplicación.
    ///   * Emisión retroactiva está permitida por normativa; aquí sólo se exige
    ///     que el vencimiento no sea previo a la emisión.
    /// </summary>
    public sealed record FechaVencimiento
    {
        /// <summary>Fecha en formato de día (sin hora).</summary>
        public DateOnly Value { get; }

        private FechaVencimiento(DateOnly value) => Value = value;

        // ===================== Fábricas generales =====================

        /// <summary>
        /// Crea un vencimiento validando que sea >= emisión.
        /// </summary>
        public static FechaVencimiento Create(DateOnly fechaEmision, DateOnly fechaVencimiento)
        {
            if (fechaVencimiento < fechaEmision)
                throw new FechaInvalidaException("La fecha de vencimiento no puede ser anterior a la fecha de emisión.", nameof(fechaVencimiento));

            return new(fechaVencimiento);
        }

        /// <summary>
        /// Vencimiento igual a emisión (caso por defecto al abrir el formulario).
        /// </summary>
        public static FechaVencimiento IgualAEmision(DateOnly fechaEmision) => new(fechaEmision);

        /// <summary>
        /// Vencimiento calculado a partir de días de crédito (días &gt; 0).
        /// </summary>
        public static FechaVencimiento DesdeDiasCredito(DateOnly fechaEmision, int dias)
        {
                if (dias <= 0) throw new FechaInvalidaException("Los días de crédito deben ser > 0");
            return new(fechaEmision.AddDays(dias));
        }

        /// <summary>
        /// Versión que no lanza excepciones. Devuelve false si no cumple las reglas.
        /// </summary>
        public static bool TryCreate(DateOnly fechaEmision, DateOnly fechaVencimiento, out FechaVencimiento? result)
        {
            try { result = Create(fechaEmision, fechaVencimiento); return true; }
            catch { result = null; return false; }
        }

        // ===================== Integración con FormaDePago =====================

        /// <summary>
        /// Calcula un vencimiento coherente con la forma de pago:
        /// - CONTADO: vencimiento == emisión (ignora días si se pasan).
        /// - CRÉDITO: requiere díasCredito &gt; 0.
        /// </summary>
        public static FechaVencimiento ParaFormaDePago(FormaDePago forma, DateOnly fechaEmision, int? diasCredito = null)
        {
            if (forma is null) throw new ArgumentNullException(nameof(forma));

            if (forma.EsContado)
            {
                // En CONTADO, el vencimiento es el mismo día de emisión.
                return IgualAEmision(fechaEmision);
            }

            // Crédito
            if (!diasCredito.HasValue || diasCredito.Value <= 0)
                throw new FechaInvalidaException("Para CRÉDITO debe indicarse días de crédito > 0.", nameof(diasCredito));

            return DesdeDiasCredito(fechaEmision, diasCredito.Value);
        }

        // ===================== Consultas útiles =====================

        /// <summary>¿El vencimiento es exactamente el mismo día que la emisión?</summary>
        public bool EsMismoDiaQue(DateOnly fechaEmision) => Value == fechaEmision;

        /// <summary>Días desde emisión hasta vencimiento (0 si igual).</summary>
        public int DiasDesde(DateOnly fechaEmision) => (Value.ToDateTime(TimeOnly.MinValue) - fechaEmision.ToDateTime(TimeOnly.MinValue)).Days;

        /// <summary>Representación ISO (yyyy-MM-dd) útil para XML/UBL.</summary>
        public override string ToString() => Value.ToString("yyyy-MM-dd");
    }
}

using System;
using System.Collections.Generic;

namespace ComprobantesElectronicosBC.Domain.ValueObjects
{
    /// <summary>
    /// Forma de pago normativa (SUNAT/UBL PaymentMeansCode):
    /// - "10" Contado  (puede indicar método visible en UI/PDF)
    /// - "20" Crédito  (sin método aquí; cuotas se modelan aparte)
    ///
    /// Nota: qué método aparece “por defecto” se define fuera del VO
    /// (p.ej., ConfiguracionSistemaBC). Este VO sólo representa el valor.
    /// </summary>
    public sealed record FormaDePago
    {
        // Códigos normativos
        public const string CONTADO = "10";
        public const string CREDITO = "20";

        /// <summary>"10" (Contado) o "20" (Crédito).</summary>
        public string PaymentMeansCode { get; }

        /// <summary>
        /// Método de cobro (sólo para CONTADO). Mayúsculas, sin espacios extremos.
        /// Ej.: CONTADO, EFECTIVO, TARJETA, TRANSFERENCIA, YAPE, PLIN, DEPOSITO, CHEQUE, BCP, INTERBANK, OTRO.
        /// Es null en CRÉDITO.
        /// </summary>
        public string? MetodoCodigo { get; }

        /// <summary>
        /// Etiqueta legible opcional (si es null, se usa MetodoCodigo para mostrar).
        /// </summary>
        public string? MetodoNombre { get; }

        private FormaDePago(string code, string? metodoCodigo, string? metodoNombre)
        {
            PaymentMeansCode = code;
            MetodoCodigo = metodoCodigo;
            MetodoNombre = metodoNombre;
        }

        // ===== Catálogo de métodos comunes (puedes ampliarlo sin romper el dominio) =====
        private static readonly HashSet<string> MetodosPredefinidos = new(StringComparer.Ordinal)
        {
            // Se incluye explícitamente "CONTADO" como opción del combo junto a los demás métodos.
            "CONTADO", "EFECTIVO", "TARJETA", "TRANSFERENCIA", "YAPE", "PLIN", "DEPOSITO", "CHEQUE",
            // canales/etiquetas frecuentes en UI:
            "BCP", "INTERBANK", "BBVA", "SCOTIABANK",
            // comodín:
            "OTRO"
        };

        // =================== Fábricas ===================

        /// <summary>
        /// CONTADO con método del catálogo (validado). Ej.: "CONTADO", "EFECTIVO", "YAPE".
        /// </summary>
        public static FormaDePago ContadoPredefinido(string metodoCodigo, string? metodoNombre = null)
        {
            var code = NormalizeCode(CONTADO);
            var met = NormalizeMethod(metodoCodigo);

            if (!MetodosPredefinidos.Contains(met))
                throw new ArgumentException($"Método no permitido: {met}. Usa ContadoPersonalizado para “+ Nuevo”.", nameof(metodoCodigo));

            var nombre = string.IsNullOrWhiteSpace(metodoNombre) ? null : metodoNombre.Trim();
            ValidateLengths(met, nombre);
            return new(code, met, nombre);
        }

        /// <summary>
        /// CONTADO con método personalizado (para “+ Nuevo”). Se normaliza a MAYÚSCULAS.
        /// </summary>
        public static FormaDePago ContadoPersonalizado(string metodoCodigo, string? metodoNombre = null)
        {
            var code = NormalizeCode(CONTADO);
            var met = NormalizeMethod(metodoCodigo);
            if (string.IsNullOrEmpty(met))
                throw new ArgumentException("El método de cobro es obligatorio en CONTADO.", nameof(metodoCodigo));

            var nombre = string.IsNullOrWhiteSpace(metodoNombre) ? null : metodoNombre.Trim();
            ValidateLengths(met, nombre);
            return new(code, met, nombre);
        }

        /// <summary>
        /// Atajo: CONTADO con método "CONTADO" (para quienes usan literalmente esa opción en el combo).
        /// </summary>
        public static FormaDePago Contado() => ContadoPredefinido("CONTADO");

        /// <summary>Atajo: CONTADO con EFECTIVO.</summary>
        public static FormaDePago ContadoEfectivo() => ContadoPredefinido("EFECTIVO");

        /// <summary>CRÉDITO (sin método ni montos aquí).</summary>
        public static FormaDePago Credito()
        {
            var code = NormalizeCode(CREDITO);
            return new(code, null, null);
        }

        // =================== Consultas ===================
        public bool EsContado => PaymentMeansCode == CONTADO;
        public bool EsCredito => PaymentMeansCode == CREDITO;

        public override string ToString()
            => EsContado
                ? $"Contado ({MetodoNombre ?? MetodoCodigo})"
                : "Crédito";

        // =================== Helpers ===================
        private static string NormalizeCode(string raw)
        {
            var s = string.IsNullOrWhiteSpace(raw) ? "" : raw.Trim();
            if (s != CONTADO && s != CREDITO)
                throw new ArgumentException("PaymentMeansCode inválido. Use \"10\" (Contado) o \"20\" (Crédito).");
            return s;
        }

        private static string NormalizeMethod(string raw)
            => string.IsNullOrWhiteSpace(raw) ? "" : raw.Trim().ToUpperInvariant();

        private static void ValidateLengths(string metodo, string? nombre)
        {
            if (metodo.Length > 30)
                throw new ArgumentException("El código del método no debe exceder 30 caracteres.", nameof(metodo));
            if (nombre is { Length: > 60 })
                throw new ArgumentException("El nombre del método no debe exceder 60 caracteres.", nameof(nombre));
        }

        // =================== Otros atajos cómodos (UI) ===================
        public static FormaDePago ContadoTarjeta(string? etiqueta = null)        => ContadoPredefinido("TARJETA", etiqueta);
        public static FormaDePago ContadoTransferencia(string? etiqueta = null)  => ContadoPredefinido("TRANSFERENCIA", etiqueta);
        public static FormaDePago ContadoYape(string? etiqueta = null)           => ContadoPredefinido("YAPE", etiqueta);
        public static FormaDePago ContadoPlin(string? etiqueta = null)           => ContadoPredefinido("PLIN", etiqueta);
        public static FormaDePago ContadoDeposito(string? etiqueta = null)       => ContadoPredefinido("DEPOSITO", etiqueta);
        public static FormaDePago ContadoBcp(string? etiqueta = null)            => ContadoPredefinido("BCP", etiqueta);
        public static FormaDePago ContadoInterbank(string? etiqueta = null)      => ContadoPredefinido("INTERBANK", etiqueta);
    }
}

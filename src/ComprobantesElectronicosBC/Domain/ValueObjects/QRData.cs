using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ComprobantesElectronicosBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object para el contenido del QR SUNAT (CPE).
    ///
    /// Formato típico (campos separados por '|'):
    ///   RUC_EMISOR | TIPO_CPE | SERIE | NUMERO(8) | MONTO_IGV | IMPORTE_TOTAL | FECHA(YYYY-MM-DD) | TIPO_DOC_ADQ | NUM_DOC_ADQ | DIGEST_VALUE
    ///
    /// Notas de diseño:
    /// - Este VO valida y normaliza los campos y te entrega el payload exacto que debe codificarse como QR.
    /// - NO genera la imagen QR (eso va en Adapters/UI).
    /// - Permite dejar TIPO_DOC_ADQ/NUM_DOC_ADQ como "-" cuando no aplique (Boleta sin identificación, clientes varios, etc.).
    /// - DIGEST_VALUE (hash del XML firmado) puede no estar disponible al momento de construir el borrador: si no lo informas, se coloca "-".
    /// </summary>
    public sealed record QRData
    {
        // ===== Campos obligatorios =====
        public string RucEmisor { get; }          // 11 dígitos, con DV válido
        public string TipoCpe { get; }            // "01","03","07","08" (ajustable)
        public string Serie { get; }              // [A-Z0-9]{1,4}, p.ej. F001, B001, E001, NC01
        public int Numero { get; }                // 1..99_999_999
        public decimal MontoIgv { get; }          // >= 0 (2 decimales)
        public decimal ImporteTotal { get; }      // >= 0 (2 decimales)
        public DateOnly FechaEmision { get; }     // fecha de emisión (se imprime como YYYY-MM-DD)

        // ===== Opcionales/derivados =====
        public string? TipoDocAdquiriente { get; }    // Cat. 06 (ej. "6" RUC, "1" DNI) o null
        public string? NumDocAdquiriente { get; }     // Solo dígitos si se informa; o null
        public string? DigestValue { get; }           // Base64 del hash del XML firmado; o null si no disponible

        private QRData(
            string rucEmisor,
            string tipoCpe,
            string serie,
            int numero,
            decimal montoIgv,
            decimal importeTotal,
            DateOnly fechaEmision,
            string? tipoDocAdq,
            string? numDocAdq,
            string? digestValue)
        {
            RucEmisor       = rucEmisor;
            TipoCpe         = tipoCpe;
            Serie           = serie;
            Numero          = numero;
            MontoIgv        = montoIgv;
            ImporteTotal    = importeTotal;
            FechaEmision    = fechaEmision;
            TipoDocAdquiriente = tipoDocAdq;
            NumDocAdquiriente  = numDocAdq;
            DigestValue        = digestValue;
        }

        // ================== Fábrica principal ==================

        /// <summary>
        /// Crea un QRData válido y normalizado para codificar a QR.
        /// - Valida RUC (11 dígitos con dígito verificador).
        /// - Valida TipoCpe (por defecto: 01/03/07/08).
        /// - Serie: 1..4 A-Z/0-9 (se normaliza a MAYÚSCULAS).
        /// - Número: 1..99'999'999 (se presentará con 8 dígitos en el payload).
        /// - Montos: redondeo a 2 decimales usando AwayFromZero (moneda).
        /// - Fecha: se serializa como "YYYY-MM-DD".
        /// - Tipo/Num Doc Adquiriente: si no se informan, se envían como "-".
        /// - DigestValue: si no se informa o no está disponible, se envía como "-".
        /// </summary>
        public static QRData Create(
            string rucEmisor,
            string tipoCpe,
            string serie,
            int numero,
            decimal montoIgv,
            decimal importeTotal,
            DateOnly fechaEmision,
            string? tipoDocAdquiriente = null,
            string? numDocAdquiriente  = null,
            string? digestValue        = null)
        {
            // RUC
            var ruc = (rucEmisor ?? throw new ArgumentNullException(nameof(rucEmisor))).Trim();
            if (!Regex.IsMatch(ruc, @"^\d{11}$"))
                throw new ArgumentException("El RUC del emisor debe tener 11 dígitos.", nameof(rucEmisor));
            if (!EsRucValido(ruc))
                throw new ArgumentException("RUC del emisor inválido (dígito verificador).", nameof(rucEmisor));

            // Tipo CPE (ajusta el set si manejas más tipos)
            var t = (tipoCpe ?? throw new ArgumentNullException(nameof(tipoCpe))).Trim();
            if (t is not ("01" or "03" or "07" or "08"))
                throw new ArgumentException("TipoCpe inválido. Use \"01\",\"03\",\"07\" o \"08\".", nameof(tipoCpe));

            // Serie
            var s = (serie ?? throw new ArgumentNullException(nameof(serie))).Trim().ToUpperInvariant();
            if (!Regex.IsMatch(s, "^[A-Z0-9]{1,4}$"))
                throw new ArgumentException("La serie debe ser 1..4 caracteres alfanuméricos (A-Z/0-9).", nameof(serie));

            // Número
            if (numero < 1 || numero > 99_999_999)
                throw new ArgumentOutOfRangeException(nameof(numero), "El número debe estar entre 1 y 99,999,999.");

            // Montos
            if (montoIgv < 0m) throw new ArgumentOutOfRangeException(nameof(montoIgv), "El IGV no puede ser negativo.");
            if (importeTotal < 0m) throw new ArgumentOutOfRangeException(nameof(importeTotal), "El Importe Total no puede ser negativo.");
            var igv2    = Redondeo2(montoIgv);
            var total2  = Redondeo2(importeTotal);

            // Doc adquiriente (permitimos vacío -> "-"). Si viene, normalizamos y validamos.
            var tipoAdq = string.IsNullOrWhiteSpace(tipoDocAdquiriente) ? null : tipoDocAdquiriente.Trim();
            if (tipoAdq is not null && !Regex.IsMatch(tipoAdq, @"^\d{1,2}$"))
                throw new ArgumentException("Tipo de documento del adquiriente inválido (Cat.06 dígitos).", nameof(tipoDocAdquiriente));

            var numAdq = string.IsNullOrWhiteSpace(numDocAdquiriente) ? null : Regex.Replace(numDocAdquiriente!, @"\D", "");
            if (numAdq is not null && numAdq.Length is < 1 or > 15)
                throw new ArgumentException("Número de documento del adquiriente inválido (1..15 dígitos).", nameof(numDocAdquiriente));

            // DigestValue (Base64) si viene
            var digest = string.IsNullOrWhiteSpace(digestValue) ? null : digestValue.Trim();
            if (digest is not null && !EsBase64(digest))
                throw new ArgumentException("DigestValue debe ser Base64 válido.", nameof(digestValue));

            return new QRData(ruc, t, s, numero, igv2, total2, fechaEmision, tipoAdq, numAdq, digest);
        }

        // ================== Salidas ==================

        /// <summary>Número con padding de 8 dígitos (para el payload).</summary>
        public string Numero8 => Numero.ToString("00000000", CultureInfo.InvariantCulture);

        /// <summary>Fecha en formato ISO corto (YYYY-MM-DD).</summary>
        public string FechaIso => FechaEmision.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        /// <summary>Monto IGV con 2 decimales y punto decimal.</summary>
        public string Igv2 => FormatoMoneda(MontoIgv);

        /// <summary>Importe total con 2 decimales y punto decimal.</summary>
        public string Total2 => FormatoMoneda(ImporteTotal);

        /// <summary>
        /// Construye el payload oficial para QR:
        /// RUC|TIPO|SERIE|NUM(8)|IGV|TOTAL|FECHA|TIPO_DOC_ADQ|NUM_DOC_ADQ|DIGEST
        /// Si no hay doc adquiriente o hash, se envía "-".
        /// </summary>
        public string Payload
        {
            get
            {
                var tipoAdq = string.IsNullOrWhiteSpace(TipoDocAdquiriente) ? "-" : TipoDocAdquiriente!;
                var numAdq  = string.IsNullOrWhiteSpace(NumDocAdquiriente)  ? "-" : NumDocAdquiriente!;
                var hash    = string.IsNullOrWhiteSpace(DigestValue)        ? "-" : DigestValue!;
                return string.Join("|", new[]
                {
                    RucEmisor, TipoCpe, Serie, Numero8, Igv2, Total2, FechaIso, tipoAdq, numAdq, hash
                });
            }
        }

        /// <summary>Payload en UTF-8 listo para pasar al generador de QR.</summary>
        public byte[] PayloadBytes => Encoding.UTF8.GetBytes(Payload);

        public override string ToString() => Payload;

        // ================== Helpers internos ==================

        private static decimal Redondeo2(decimal v)
            => Math.Round(v, 2, MidpointRounding.AwayFromZero);

        private static string FormatoMoneda(decimal v)
            => v.ToString("0.00", CultureInfo.InvariantCulture);

        private static bool EsBase64(string s)
        {
            try { Convert.FromBase64String(s); return true; }
            catch { return false; }
        }

        // Algoritmo SUNAT (módulo 11) para RUC (igual al usado en otros VOs; repetido aquí para no acoplar).
        private static bool EsRucValido(string ruc11)
        {
            int[] pesos = { 5,4,3,2,7,6,5,4,3,2 };
            int suma = 0;
            for (int i = 0; i < 10; i++) suma += (ruc11[i] - '0') * pesos[i];
            int resto = suma % 11;
            int digito = 11 - resto;
            if (digito == 10) digito = 0;
            else if (digito == 11) digito = 1;
            return digito == (ruc11[10] - '0');
        }
    }
}

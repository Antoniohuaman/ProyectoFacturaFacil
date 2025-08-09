using System;
using System.Collections.Generic;
using System.Linq;

namespace ComprobantesElectronicosBC.Domain.ValueObjects
{
    /// <summary>
    /// CDR (Constancia de Recepción) de SUNAT.
    /// Es un Value Object: inmutable, sin identidad propia; su valor es su contenido.
    /// - Se construye al recibir la respuesta de SUNAT (emisión o baja).
    /// - Sirve para decidir el estado del comprobante (Accepted / Rejected / NeedsCorrection).
    /// Códigos típicos:
    ///   0   = Aceptado
    ///   98  = Aceptado con observaciones
    ///   100–199 = Errores/Incidencias de comunicación/proceso (no final)
    ///   2000–3999 = Rechazado
    /// </summary>
    public sealed record CDR
    {
        // ---------------------------
        // Datos mínimos y de auditoría
        // ---------------------------

        /// <summary>Código de respuesta de SUNAT (0, 98, 2000..3999, etc.).</summary>
        public int CodigoRespuesta { get; }

        /// <summary>Mensaje principal/descrición del resultado (texto SUNAT).</summary>
        public string Mensaje { get; }

        /// <summary>Fecha/hora (zona del servidor) en que se registró la respuesta.</summary>
        public DateTimeOffset FechaHoraRespuesta { get; }

        /// <summary>Observaciones/notas (puede venir vacío). 98 suele traer observaciones.</summary>
        public IReadOnlyList<string> Notas { get; }

        /// <summary>Número de ticket si aplica (p.ej., resumen diario/baja); puede ser null.</summary>
        public string? NumeroTicket { get; }

        /// <summary>Nombre del ZIP de CDR (si el gateway lo retorna y decides guardarlo).</summary>
        public string? NombreArchivoZip { get; }

        /// <summary>Contenido ZIP del CDR (binario). Null si no se guarda.</summary>
        public ReadOnlyMemory<byte>? ArchivoZip { get; }

        // ---------------------------
        // Metadatos opcionales (útiles para trazabilidad/auditoría)
        // ---------------------------
        public string? RucEmisor { get; }
        public string? TipoCpe { get; }  // "01", "03", etc.
        public string? Serie { get; }
        public string? Numero { get; }

        // ---------------------------
        // Derivados de negocio (categorización de código)
        // ---------------------------

        /// <summary>True si el CPE fue aceptado sin observaciones (código 0).</summary>
        public bool EsAceptadoSinObservaciones => CodigoRespuesta == 0;

        /// <summary>True si el CPE fue aceptado con observaciones (código 98).</summary>
        public bool EsAceptadoConObservaciones => CodigoRespuesta == 98;

        /// <summary>True si el CPE fue aceptado (0 o 98).</summary>
        public bool EsAceptado => EsAceptadoSinObservaciones || EsAceptadoConObservaciones;

        /// <summary>True si la respuesta es rechazo formal (2000..3999).</summary>
        public bool EsRechazado => CodigoRespuesta >= 2000 && CodigoRespuesta <= 3999;

        /// <summary>True si la respuesta indica problemas/errores de comunicación/proceso (100..199).</summary>
        public bool EsErrorComunicacion => CodigoRespuesta >= 100 && CodigoRespuesta <= 199;

        /// <summary>True si trae archivo CDR (ZIP) adjunto.</summary>
        public bool TieneArchivoZip => ArchivoZip is { Length: > 0 };

        /// <summary>True si la respuesta es final (Aceptado o Rechazado).</summary>
        public bool EsResultadoFinal => EsAceptado || EsRechazado;

        // ---------------------------
        // Construcción y fábricas
        // ---------------------------

        private CDR(
            int codigoRespuesta,
            string mensaje,
            DateTimeOffset fechaHoraRespuesta,
            IReadOnlyList<string> notas,
            string? numeroTicket,
            string? nombreArchivoZip,
            ReadOnlyMemory<byte>? archivoZip,
            string? rucEmisor,
            string? tipoCpe,
            string? serie,
            string? numero)
        {
            CodigoRespuesta = codigoRespuesta;
            Mensaje = mensaje;
            FechaHoraRespuesta = fechaHoraRespuesta;
            Notas = notas;
            NumeroTicket = numeroTicket;
            NombreArchivoZip = nombreArchivoZip;
            ArchivoZip = archivoZip;
            RucEmisor = rucEmisor;
            TipoCpe = tipoCpe;
            Serie = serie;
            Numero = numero;
        }

        /// <summary>
        /// Fábrica principal con validaciones de invariantes.
        /// </summary>
        public static CDR Create(
            int codigoRespuesta,
            string mensaje,
            DateTimeOffset? fechaHoraRespuesta = null,
            IEnumerable<string>? notas = null,
            string? numeroTicket = null,
            string? nombreArchivoZip = null,
            ReadOnlyMemory<byte>? archivoZip = null,
            string? rucEmisor = null,
            string? tipoCpe = null,
            string? serie = null,
            string? numero = null)
        {
            var msg = string.IsNullOrWhiteSpace(mensaje) ? throw new ArgumentException("Mensaje es obligatorio.", nameof(mensaje))
                                                         : mensaje.Trim();

            var ts = fechaHoraRespuesta ?? DateTimeOffset.UtcNow;

            var normalizedNotas = (notas ?? Enumerable.Empty<string>())
                .Select(n => (n ?? string.Empty).Trim())
                .Where(n => n.Length > 0)
                .ToArray();

            // Invariantes de archivo ZIP (si guardas el CDR firmado de SUNAT)
            if (archivoZip is { Length: > 0 } && string.IsNullOrWhiteSpace(nombreArchivoZip))
                throw new ArgumentException("Si se provee ArchivoZip, NombreArchivoZip es obligatorio.", nameof(nombreArchivoZip));

            if ((!archivoZip.HasValue || archivoZip.Value.Length == 0) && !string.IsNullOrWhiteSpace(nombreArchivoZip))
                throw new ArgumentException("No se puede definir NombreArchivoZip sin ArchivoZip.", nameof(nombreArchivoZip));

            // Nota: No limitamos rango de código para permitir futuros códigos SUNAT,
            // pero la categorización (Aceptado/Rechazado/etc.) se basa en rangos conocidos.

            return new CDR(
                codigoRespuesta: codigoRespuesta,
                mensaje: msg,
                fechaHoraRespuesta: ts,
                notas: normalizedNotas,
                numeroTicket: string.IsNullOrWhiteSpace(numeroTicket) ? null : numeroTicket.Trim(),
                nombreArchivoZip: string.IsNullOrWhiteSpace(nombreArchivoZip) ? null : nombreArchivoZip.Trim(),
                archivoZip: archivoZip,
                rucEmisor: string.IsNullOrWhiteSpace(rucEmisor) ? null : rucEmisor.Trim(),
                tipoCpe: string.IsNullOrWhiteSpace(tipoCpe) ? null : tipoCpe.Trim(),
                serie: string.IsNullOrWhiteSpace(serie) ? null : serie.Trim(),
                numero: string.IsNullOrWhiteSpace(numero) ? null : numero.Trim()
            );
        }

        /// <summary>Atajo: crea un CDR de Aceptado (0) con notas opcionales.</summary>
        public static CDR CreateAceptado(string mensaje = "Aceptado", IEnumerable<string>? notas = null)
            => Create(0, mensaje, notas: notas);

        /// <summary>Atajo: crea un CDR de Aceptado con Observaciones (98).</summary>
        public static CDR CreateAceptadoConObservaciones(string mensaje = "Aceptado con observaciones", IEnumerable<string>? notas = null)
            => Create(98, mensaje, notas: notas);

        /// <summary>Atajo: crea un CDR de Rechazado (código 2000..3999). Valida el rango.</summary>
        public static CDR CreateRechazado(int codigo, string mensaje, IEnumerable<string>? notas = null)
        {
            if (codigo < 2000 || codigo > 3999)
                throw new ArgumentOutOfRangeException(nameof(codigo), "Código de rechazo debe estar entre 2000 y 3999.");
            return Create(codigo, mensaje, notas: notas);
        }

        /// <summary>
        /// Devuelve una copia con el ZIP de CDR adjunto (valida nombre y contenido).
        /// </summary>
        public CDR WithArchivoZip(string nombreArchivoZip, ReadOnlyMemory<byte> archivoZip)
        {
            if (string.IsNullOrWhiteSpace(nombreArchivoZip))
                throw new ArgumentException("NombreArchivoZip es obligatorio.", nameof(nombreArchivoZip));
            if (archivoZip.Length == 0)
                throw new ArgumentException("ArchivoZip vacío.", nameof(archivoZip));

            return new CDR(
                CodigoRespuesta,
                Mensaje,
                FechaHoraRespuesta,
                Notas,
                NumeroTicket,
                nombreArchivoZip.Trim(),
                archivoZip,
                RucEmisor,
                TipoCpe,
                Serie,
                Numero
            );
        }

        // ---------------------------
        // Utilidades
        // ---------------------------

        /// <summary>Resumen legible útil para logs/monitoring.</summary>
        public string ToResumen()
        {
            var estado = EsAceptado ? (EsAceptadoConObservaciones ? "ACEPTADO_CON_OBS" : "ACEPTADO")
                       : EsRechazado ? "RECHAZADO"
                       : EsErrorComunicacion ? "ERROR_COMUNICACION"
                       : "OTRO";

            var idDoc = (TipoCpe, Serie, Numero) switch
            {
                (not null, not null, not null) => $"{TipoCpe}-{Serie}-{Numero}",
                _ => "s/d"
            };

            var notas = Notas.Count > 0 ? $" | Notas: {string.Join(" | ", Notas)}" : string.Empty;
            return $"[{estado}] Cod={CodigoRespuesta} Doc={idDoc} Msg='{Mensaje}'{notas}";
        }
    }
}

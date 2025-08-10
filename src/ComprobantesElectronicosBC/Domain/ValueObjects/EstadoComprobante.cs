using System;
using System.Collections.Generic;

namespace ComprobantesElectronicosBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object que modela el estado de un comprobante dentro del BC de Comprobantes.
    /// 
    /// - Es inmutable e igual por valor (record): perfecto para estados.
    /// - Define capacidades por estado (qué permite la UI/lógica hacer).
    /// - Expone transiciones válidas de negocio (invariantes del ciclo de vida).
    /// - Incluye un mapeo sugerido desde códigos de respuesta SUNAT (CDR/errores).
    ///
    /// Estados modelados (equivalencias UX):
    ///   DR = Draft (Borrador)
    ///   PE = PendingValidation (Enviado, esperando validación/resultado)
    ///   NC = NeedsCorrection   (Corregir: rehacer y reenviar)
    ///   AC = Accepted          (Aceptado por SUNAT, CDR OK)
    ///   RJ = Rejected          (Rechazado por SUNAT, CDR con observaciones fatales)
    ///   CN = Cancelled         (Anulado/Baja confirmada)
    ///
    /// Nota:
    ///  - "EsFinal" indica que el documento ya no es editable en UI. Aun así,
    ///    Accepted permite la transición a Cancelled (baja) por normativa.
    ///  - La firma/envío es responsabilidad de un servicio externo; aquí solo gestionamos el estado.
    /// </summary>
    public sealed record EstadoComprobante
    {
        /// <summary>Código corto interno para persistencia/UX (DR, PE, NC, AC, RJ, CN).</summary>
        public string Code { get; }

        /// <summary>Nombre legible del estado (Draft, PendingValidation, NeedsCorrection, ...).</summary>
        public string Name { get; }

        // -------- Capacidades (ayudan a la UI y a las invariantes de dominio) --------
        public bool PuedeEditar { get; }
        public bool PuedeEliminar { get; }
        public bool PuedeEmitir { get; }
        public bool PuedeReintentarEnvio { get; }
        public bool PuedeAnular { get; }
        /// <summary>Estados finales (no editables): Accepted, Rejected, Cancelled.</summary>
        public bool EsFinal { get; }

        private EstadoComprobante(
            string code, string name,
            bool puedeEditar, bool puedeEliminar, bool puedeEmitir,
            bool puedeReintentarEnvio, bool puedeAnular, bool esFinal)
        {
            Code = code;
            Name = name;
            PuedeEditar = puedeEditar;
            PuedeEliminar = puedeEliminar;
            PuedeEmitir = puedeEmitir;
            PuedeReintentarEnvio = puedeReintentarEnvio;
            PuedeAnular = puedeAnular;
            EsFinal = esFinal;
        }

        // ------------------- Instancias canónicas del VO -------------------
        // Borrador: editable, eliminable, permite "Emitir" → pasa a PE.
        public static EstadoComprobante Draft { get; } = new(
            code: "DR", name: "Draft",
            puedeEditar: true,  puedeEliminar: true,  puedeEmitir: true,
            puedeReintentarEnvio: false, puedeAnular: false, esFinal: false);

        // Enviado/Pendiente: bloqueado mientras responde el API/SUNAT; permite reintento.
        public static EstadoComprobante PendingValidation { get; } = new(
            code: "PE", name: "PendingValidation",
            puedeEditar: false, puedeEliminar: false, puedeEmitir: false,
            puedeReintentarEnvio: true, puedeAnular: false, esFinal: false);

        // Corregir: editable, permite reenviar (re-intentar) → vuelve a PE.
        public static EstadoComprobante NeedsCorrection { get; } = new(
            code: "NC", name: "NeedsCorrection",
            puedeEditar: true,  puedeEliminar: false, puedeEmitir: true,
            puedeReintentarEnvio: true, puedeAnular: false, esFinal: false);

        // Aceptado: final (no editable). Por normativa, puede ir a Cancelled (baja).
        public static EstadoComprobante Accepted { get; } = new(
            code: "AC", name: "Accepted",
            puedeEditar: false, puedeEliminar: false, puedeEmitir: false,
            puedeReintentarEnvio: false, puedeAnular: true, esFinal: true);

        // Rechazado: final (no editable). Suele originar NC/ND o baja por otro flujo.
        public static EstadoComprobante Rejected { get; } = new(
            code: "RJ", name: "Rejected",
            puedeEditar: false, puedeEliminar: false, puedeEmitir: false,
            puedeReintentarEnvio: false, puedeAnular: false, esFinal: true);

        // Anulado: final definitivo.
        public static EstadoComprobante Cancelled { get; } = new(
            code: "CN", name: "Cancelled",
            puedeEditar: false, puedeEliminar: false, puedeEmitir: false,
            puedeReintentarEnvio: false, puedeAnular: false, esFinal: true);

        // Acceso por código (útil para persistencia/DTOs)
        private static readonly Dictionary<string, EstadoComprobante> ByCode =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["DR"] = Draft,
                ["PE"] = PendingValidation,
                ["NC"] = NeedsCorrection,
                ["AC"] = Accepted,
                ["RJ"] = Rejected,
                ["CN"] = Cancelled
            };

        /// <summary>Reconstruye un estado desde su código corto (DR/PE/NC/AC/RJ/CN).</summary>
        public static EstadoComprobante FromCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("El código de estado no puede ser vacío.", nameof(code));

            if (!ByCode.TryGetValue(code.Trim(), out var st))
                throw new ArgumentException($"Código de estado inválido: {code}. Use DR, PE, NC, AC, RJ o CN.", nameof(code));

            return st;
        }

        // ------------------- Transiciones de negocio permitidas -------------------
        // Definimos las transiciones válidas explícitamente para preservar invariantes.
        private static readonly Dictionary<EstadoComprobante, HashSet<EstadoComprobante>> AllowedTransitions =
            new()
            {
                // Draft → PendingValidation
                [Draft] = new() { PendingValidation },

                // PendingValidation → Accepted | Rejected | NeedsCorrection
                [PendingValidation] = new() { Accepted, Rejected, NeedsCorrection },

                // NeedsCorrection → PendingValidation
                [NeedsCorrection] = new() { PendingValidation },

                // Accepted → Cancelled (baja)
                [Accepted] = new() { Cancelled },

                // Rejected → (sin transición directa; se gestiona NC/ND/baja por otros flujos)
                [Rejected] = new(),

                // Cancelled → (terminal)
                [Cancelled] = new()
            };

        /// <summary>
        /// Verifica si una transición está permitida según las reglas de negocio.
        /// </summary>
        public bool CanTransitionTo(EstadoComprobante target)
            => AllowedTransitions.TryGetValue(this, out var set) && set.Contains(target);

        /// <summary>
        /// Devuelve el nuevo estado si la transición es válida; de lo contrario lanza InvalidOperationException.
        /// </summary>
        public EstadoComprobante TransitionTo(EstadoComprobante target)
        {
            if (!CanTransitionTo(target))
                throw new InvalidOperationException($"Transición no permitida: {Name} → {target.Name}.");
            return target;
        }

        // ------------------- Mapeo desde respuesta SUNAT -------------------
        /// <summary>
        /// Sugerencia de siguiente estado en base a un código de respuesta técnica:
        /// - "0" o "98" → Accepted
        /// - 2000..3999  → Rejected (errores de validación SUNAT)
        /// - 0100..0199  → PendingValidation (errores de comunicación/recepción: reintento)
        /// - null/vacío/otro → NeedsCorrection
        /// 
        /// Se usa para guiar el flujo tras invocar al API externo. La decisión final
        /// (p. ej., reintentar o corregir) la toma la Aplicación/Usuario.
        /// </summary>
        public static EstadoComprobante SiguienteDesdeRespuestaSunat(string? responseCode)
        {
            // Normalizamos
            var s = (responseCode ?? "").Trim();

            // Aceptado
            if (s == "0" || s == "98") return Accepted;

            // Rango numérico
            if (int.TryParse(s, out var n))
            {
                if (n >= 2000 && n <= 3999) return Rejected;          // errores de validación SUNAT
                if (n >= 100 && n <= 199)   return PendingValidation; // incidencias de comunicación → reintento
            }

            // Cualquier otra cosa: requiere corrección manual
            return NeedsCorrection;
        }

        public override string ToString() => Name;
    }
}

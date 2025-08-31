using System;
using SharedKernel.Exceptions;

namespace SharedKernel.ValueObjects
{
    /// <summary>
    /// Identidad opaca de Establecimiento.
    /// - Wrapper sobre Guid (no vacío).
    /// - No usar el “código” visible como ID; el código es dato editable.
    /// </summary>
    public sealed record EstablecimientoId
    {
        /// <summary>Guid canónico del identificador.</summary>
        public Guid Value { get; }

        private EstablecimientoId(Guid value) => Value = value;

        /// <summary>Crea un EstablecimientoId desde un Guid (no Empty).</summary>
        public static EstablecimientoId From(Guid guid)
        {
            if (guid == Guid.Empty)
                throw new BusinessRuleException("EstablecimientoId no puede ser Guid.Empty.");
            return new EstablecimientoId(guid);
        }

        /// <summary>Genera un nuevo EstablecimientoId.</summary>
        public static EstablecimientoId New() => new EstablecimientoId(Guid.NewGuid());

        /// <summary>Crea desde cadena (Guid válido).</summary>
        public static EstablecimientoId FromString(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new BusinessRuleException("EstablecimientoId es obligatorio.");
            if (!Guid.TryParse(input.Trim(), out var g) || g == Guid.Empty)
                throw new BusinessRuleException("EstablecimientoId inválido.");
            return new EstablecimientoId(g);
        }

        /// <summary>Intenta parsear sin lanzar excepción.</summary>
        public static bool TryParse(string? input, out EstablecimientoId? id)
        {
            id = null;
            if (string.IsNullOrWhiteSpace(input)) return false;
            if (!Guid.TryParse(input.Trim(), out var g) || g == Guid.Empty) return false;
            id = new EstablecimientoId(g);
            return true;
        }

        /// <summary>Indica si el Guid interno es vacío.</summary>
        public bool IsEmpty => Value == Guid.Empty;

        public override string ToString() => Value.ToString("D");

        // Conversiones ergonómicas
        public static explicit operator EstablecimientoId(Guid g) => From(g);
        public static implicit operator Guid(EstablecimientoId id) => id.Value;

        /// <summary>Comparación explícita por identidad.</summary>
        public bool EsMismoQue(EstablecimientoId otra) => otra is not null && Value == otra.Value;
    }
}

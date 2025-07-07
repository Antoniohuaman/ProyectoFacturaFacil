using System;

namespace ControlCajaBC.Domain.ValueObjects
{
    /// <summary>
    /// Marca de tiempo de eventos en la caja.
    /// </summary>
    public sealed record FechaHora
    {
        public DateTime Value { get; }

        public FechaHora(DateTime value)
        {
            if (value.Kind != DateTimeKind.Utc)
                throw new ArgumentException("La fecha debe estar en UTC.", nameof(value));

            Value = value;
        }

        public static FechaHora NowUtc() => new(DateTime.UtcNow);
    }
}

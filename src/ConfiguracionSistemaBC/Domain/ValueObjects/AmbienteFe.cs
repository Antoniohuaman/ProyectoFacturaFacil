using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ConfiguracionSistemaBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object que modela el ambiente de emisión de una empresa/tenant.
    /// Reglas:
    /// - Estados permitidos: PRUEBA, PRODUCCION.
    /// - El sistema inicia en PRUEBA.
    /// - El cambio a PRODUCCION es irreversible (no se permite volver a PRUEBA).
    /// NOTA: limpieza de datos de prueba / reinicio de correlativos sucede fuera del VO
    /// (p. ej., en Application/Infra mediante eventos del dominio).
    /// </summary>
    [DebuggerDisplay("{Value}")]
    public sealed class AmbienteFe
    {
        // Instancias canónicas
        public static readonly AmbienteFe PRUEBA     = new("PRUEBA");
        public static readonly AmbienteFe PRODUCCION = new("PRODUCCION");

        /// <summary>Conjunto de valores soportados (útil para combos/UI).</summary>
        public static IReadOnlyCollection<AmbienteFe> All { get; } = new[] { PRUEBA, PRODUCCION };

        /// <summary>Valor canónico en mayúsculas: "PRUEBA" o "PRODUCCION".</summary>
        public string Value { get; }

        private AmbienteFe(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("El valor de ambiente es obligatorio.", nameof(value));

            var v = value.Trim().ToUpperInvariant();
            if (v is not ("PRUEBA" or "PRODUCCION"))
                throw new ArgumentOutOfRangeException(nameof(value), "Ambiente debe ser PRUEBA o PRODUCCION.");

            Value = v;
        }

        /// <summary>
        /// Crea una instancia normalizando texto (case-insensitive) y devolviendo la instancia canónica.
        /// </summary>
        public static AmbienteFe Create(string value)
        {
            var v = value?.Trim().ToUpperInvariant() ?? throw new ArgumentNullException(nameof(value));
            return v == "PRUEBA" ? PRUEBA
                 : v == "PRODUCCION" ? PRODUCCION
                 : throw new ArgumentOutOfRangeException(nameof(value), "Ambiente debe ser PRUEBA o PRODUCCION.");
        }

        /// <summary>Intenta crear sin lanzar excepción.</summary>
        public static bool TryCreate(string? value, out AmbienteFe? ambiente)
        {
            ambiente = null;
            if (string.IsNullOrWhiteSpace(value)) return false;
            var v = value.Trim().ToUpperInvariant();
            ambiente = v switch
            {
                "PRUEBA" => PRUEBA,
                "PRODUCCION" => PRODUCCION,
                _ => null
            };
            return ambiente is not null;
        }

        public bool EsPrueba => ReferenceEquals(this, PRUEBA) || Value == "PRUEBA";
        public bool EsProduccion => ReferenceEquals(this, PRODUCCION) || Value == "PRODUCCION";

        /// <summary>
        /// Indica si una transición desde el estado actual hacia <paramref name="destino"/> es válida.
        /// Válidas: PRUEBA→PRUEBA, PRUEBA→PRODUCCION, PRODUCCION→PRODUCCION.
        /// Inválida: PRODUCCION→PRUEBA (irreversible).
        /// </summary>
        public bool EsTransicionValida(AmbienteFe destino)
        {
            if (destino is null) throw new ArgumentNullException(nameof(destino));
            return !(EsProduccion && destino.EsPrueba);
        }

        /// <summary>
        /// Valida (y lanza si corresponde) al intentar cambiar de <paramref name="actual"/> a <paramref name="destino"/>.
        /// </summary>
        public static void ValidarTransicion(AmbienteFe actual, AmbienteFe destino)
        {
            if (actual is null) throw new ArgumentNullException(nameof(actual));
            if (destino is null) throw new ArgumentNullException(nameof(destino));
            if (actual.EsProduccion && destino.EsPrueba)
                throw new InvalidOperationException("No es posible volver a PRUEBA después de pasar a PRODUCCION.");
        }

        // Igualdad por valor (basada en Value)
        public override bool Equals(object? obj)
            => obj is AmbienteFe other && string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

        public static bool operator ==(AmbienteFe? left, AmbienteFe? right) => Equals(left, right);
        public static bool operator !=(AmbienteFe? left, AmbienteFe? right) => !Equals(left, right);

        public override string ToString() => Value;

        /// <summary>Conversión implícita a string ("PRUEBA" | "PRODUCCION").</summary>
        public static implicit operator string(AmbienteFe value) => value.Value;

        /// <summary>Conversión explícita desde string (case-insensitive).</summary>
        public static explicit operator AmbienteFe(string value) => Create(value);
    }
}
// src/ConfiguracionSistemaBC/Domain/ValueObjects/AmbienteFe.cs
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConfiguracionSistemaBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object que modela el ambiente de emisión de la empresa (tenant).
    /// - Por defecto es PRUEBA.
    /// - El cambio a PRODUCCION es irreversible (no se permite volver a PRUEBA).
    /// - Se serializa a JSON como "PRUEBA" o "PRODUCCION".
    /// </summary>
    [JsonConverter(typeof(AmbienteFeJsonConverter))]
    public sealed record AmbienteFe
    {
        public static readonly AmbienteFe PRUEBA     = new("PRUEBA");
        public static readonly AmbienteFe PRODUCCION = new("PRODUCCION");

        /// <summary>Valor canónico en mayúsculas ("PRUEBA" | "PRODUCCION").</summary>
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

        /// <summary>Crea una instancia normalizando el texto (case-insensitive).</summary>
        public static AmbienteFe Create(string value) => new(value);

        /// <summary>Intenta crear sin lanzar excepción.</summary>
        public static bool TryCreate(string? value, out AmbienteFe? ambiente)
        {
            ambiente = null;
            if (string.IsNullOrWhiteSpace(value)) return false;
            var v = value.Trim().ToUpperInvariant();
            if (v is "PRUEBA" or "PRODUCCION") { ambiente = new AmbienteFe(v); return true; }
            return false;
        }

        public bool EsPrueba      => Value == "PRUEBA";
        public bool EsProduccion  => Value == "PRODUCCION";

        /// <summary>
        /// Verifica si es válida la transición actual → destino.
        /// Regla: una vez en PRODUCCION, no se permite volver a PRUEBA.
        /// </summary>
        public bool PuedeTransicionarA(AmbienteFe destino)
        {
            if (destino is null) throw new ArgumentNullException(nameof(destino));
            if (EsProduccion && destino.EsPrueba) return false;
            return !Value.Equals(destino.Value, StringComparison.Ordinal); // si es igual, es idempotente (no "cambio")
        }

        /// <summary>
        /// Valida (y si corresponde lanza) al intentar cambiar de <paramref name="actual"/> a <paramref name="destino"/>.
        /// </summary>
        public static void ValidarTransicion(AmbienteFe actual, AmbienteFe destino)
        {
            if (actual is null) throw new ArgumentNullException(nameof(actual));
            if (destino is null) throw new ArgumentNullException(nameof(destino));
            if (actual.EsProduccion && destino.EsPrueba)
                throw new InvalidOperationException("No es posible volver a PRUEBA después de pasar a PRODUCCION.");
        }

        public override string ToString() => Value;
    }

    /// <summary>Serializa/Deserializa a JSON como "PRUEBA" / "PRODUCCION".</summary>
    public sealed class AmbienteFeJsonConverter : JsonConverter<AmbienteFe>
    {
        public override AmbienteFe? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Acepta null para propiedades nullable
            if (reader.TokenType == JsonTokenType.Null) return null;

            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException($"Se esperaba un string para {nameof(AmbienteFe)}.");

            var raw = reader.GetString();
            if (AmbienteFe.TryCreate(raw, out var ambiente)) return ambiente;

            throw new JsonException($"Valor de ambiente no reconocido: \"{raw}\". Use \"PRUEBA\" o \"PRODUCCION\".");
        }

        public override void Write(Utf8JsonWriter writer, AmbienteFe value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Value);
        }
    }
}

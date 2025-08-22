#nullable enable
using System;
using System.Collections.Generic;

namespace SharedKernel.Exceptions
{
    /// <summary>
    /// Concurrencia optimista: la versión esperada no coincide con la actual al guardar.
    /// Mapea naturalmente a HTTP 409/412 en Adapters.
    /// </summary>
    public sealed class ConcurrencyException : DomainException
    {
        public const string DefaultCode = "CONCURRENCY_CONFLICT";

        /// <summary>Nombre lógico del agregado/entidad (p.ej. "PrecioProducto").</summary>
        public string Aggregate { get; }

        /// <summary>Identificador del agregado/entidad (string libre para Guid/Sku/etc.).</summary>
        public string? AggregateId { get; }

        /// <summary>Versión esperada por el llamador (antes del guardado).</summary>
        public int? ExpectedVersion { get; }

        /// <summary>Versión encontrada en el almacenamiento.</summary>
        public int? CurrentVersion { get; }

        public ConcurrencyException(
            string aggregate,
            string? aggregateId,
            int? expectedVersion,
            int? currentVersion,
            string? message = null,
            IReadOnlyDictionary<string, object?>? metadata = null)
            : base(DefaultCode, message ?? BuildMessage(aggregate, aggregateId, expectedVersion, currentVersion),
                   BuildMetadata(aggregate, aggregateId, expectedVersion, currentVersion, metadata))
        {
            if (string.IsNullOrWhiteSpace(aggregate))
                throw new ArgumentException("El nombre del agregado no puede ser vacío.", nameof(aggregate));

            Aggregate = aggregate;
            AggregateId = aggregateId;
            ExpectedVersion = expectedVersion;
            CurrentVersion = currentVersion;
        }

        private static string BuildMessage(string agg, string? id, int? expected, int? current)
        {
            var idPart = id is null ? "" : $" '{id}'";
            var exp = expected.HasValue ? expected.Value.ToString() : "?";
            var cur = current.HasValue ? current.Value.ToString() : "?";
            return $"Conflicto de concurrencia en {agg}{idPart}: versión esperada {exp}, versión actual {cur}.";
        }

        private static IReadOnlyDictionary<string, object?> BuildMetadata(
            string agg, string? id, int? expected, int? current, IReadOnlyDictionary<string, object?>? extra)
        {
            var dict = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["aggregate"] = agg,
                ["aggregateId"] = id,
                ["expectedVersion"] = expected,
                ["currentVersion"] = current
            };
            if (extra is not null)
                foreach (var kv in extra) dict[kv.Key] = kv.Value;
            return dict;
        }
    }
}

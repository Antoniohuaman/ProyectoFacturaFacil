#nullable enable
using System;
using System.Collections.Generic;

namespace SharedKernel.Exceptions
{
    /// <summary>
    /// Recurso/Agregado no encontrado. Útil en capa de Aplicación (p.ej. al cargar
    /// un agregado por Id/Sku) o en Dominio si una relación obligatoria no existe.
    /// </summary>
    public sealed class NotFoundException : DomainException
    {
        public const string DefaultCode = "NOT_FOUND";

        /// <summary>Nombre lógico del recurso (p.ej. "PrecioProducto", "ListaPrecio").</summary>
        public string Resource { get; }

        /// <summary>Identificador del recurso (texto libre; puede ser Guid, string, etc.).</summary>
        public string? ResourceId { get; }

        public NotFoundException(
            string resource,
            string? resourceId = null,
            string? message = null,
            IReadOnlyDictionary<string, object?>? metadata = null)
            : base(DefaultCode, message ?? BuildMessage(resource, resourceId), BuildMetadata(resource, resourceId, metadata))
        {
            if (string.IsNullOrWhiteSpace(resource))
                throw new ArgumentException("El nombre del recurso no puede ser vacío.", nameof(resource));

            Resource = resource;
            ResourceId = resourceId;
        }

        public static NotFoundException For<T>(object? id, string? message = null)
            => new(typeof(T).Name, id?.ToString(), message);

        private static string BuildMessage(string resource, string? id)
            => id is null ? $"{resource} no encontrado." : $"{resource} '{id}' no encontrado.";

        private static IReadOnlyDictionary<string, object?>? BuildMetadata(
            string resource, string? id, IReadOnlyDictionary<string, object?>? extra)
        {
            var dict = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["resource"] = resource,
                ["id"] = id
            };
            if (extra is not null)
                foreach (var kv in extra) dict[kv.Key] = kv.Value;
            return dict;
        }
    }
}

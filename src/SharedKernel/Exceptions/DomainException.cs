#nullable enable
using System;
using System.Collections.Generic;

namespace SharedKernel.Exceptions
{
    /// <summary>
    /// Base para todas las excepciones del dominio (DDD).
    /// Incluye:
    /// - <see cref="Code"/>: código estable (para logs/UI).
    /// - <see cref="OccurredOn"/>: timestamp UTC.
    /// - <see cref="Metadata"/>: datos opcionales para diagnóstico.
    /// </summary>
    public abstract class DomainException : Exception
    {
        /// <summary>Código de error estable, útil para mapear a UI/API.</summary>
        public string Code { get; }

        /// <summary>Momento (UTC) en que se creó la excepción.</summary>
        public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;

        /// <summary>Datos adicionales (solo lectura) para diagnóstico/logs.</summary>
        public IReadOnlyDictionary<string, object?>? Metadata { get; }

        protected DomainException(
            string code,
            string message,
            IReadOnlyDictionary<string, object?>? metadata = null,
            Exception? innerException = null)
            : base(message, innerException)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("El código de la excepción no puede ser vacío.", nameof(code));

            Code = code;
            Metadata = metadata;
        }

        public override string ToString()
        {
            var baseStr = base.ToString();
            return $"[{Code}] @ {OccurredOn:O} {baseStr}";
        }
    }
}

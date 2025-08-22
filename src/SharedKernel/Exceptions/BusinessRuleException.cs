#nullable enable
using System.Collections.Generic;

namespace SharedKernel.Exceptions
{
    /// <summary>
    /// Violación de una regla de negocio o invariante de dominio.
    /// Para usarla o heredarla en Agregados/Servicios de dominio cuando
    /// una operación no es válida según las reglas del negocio.
    /// </summary>
    public class BusinessRuleException : DomainException
    {
        public const string DefaultCode = "BUSINESS_RULE_VIOLATION";

        public BusinessRuleException(
            string message,
            IReadOnlyDictionary<string, object?>? metadata = null)
            : base(DefaultCode, message, metadata) { }

        public BusinessRuleException(
            string code,
            string message,
            IReadOnlyDictionary<string, object?>? metadata = null)
            : base(string.IsNullOrWhiteSpace(code) ? DefaultCode : code, message, metadata) { }
    }
}

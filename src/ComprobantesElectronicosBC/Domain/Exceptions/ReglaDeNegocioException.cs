using SharedKernel.Exceptions;

namespace ComprobantesElectronicosBC.Domain.Exceptions
{
    /// <summary>
    /// Alias local de excepción de regla de negocio. Equivalente a BusinessRuleException para semántica explícita en este BC.
    /// </summary>
    public sealed class ReglaDeNegocioException : BusinessRuleException
    {
        public ReglaDeNegocioException(string message)
            : base(message) { }

        public ReglaDeNegocioException(string code, string message)
            : base(code, message) { }
    }
}

using SharedKernel.Exceptions;

namespace ComprobantesElectronicosBC.Domain.Exceptions
{
    /// <summary>
    /// Excepción de dominio para operaciones no permitidas por el estado actual del agregado.
    /// </summary>
    public sealed class EstadoInvalidoException : BusinessRuleException
    {
        public EstadoInvalidoException(string message)
            : base(message) { }

        public EstadoInvalidoException(string code, string message)
            : base(code, message) { }
    }
}

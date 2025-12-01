using SharedKernel.Exceptions;

namespace GestionCobranzasBC.Domain.Exceptions;

/// <summary>
/// Excepción base para reglas de negocio del contexto de Gestión de Cobranzas.
/// </summary>
public abstract class GestionCobranzasException : BusinessRuleException
{
    protected GestionCobranzasException(string message)
        : base(message)
    {
    }
}

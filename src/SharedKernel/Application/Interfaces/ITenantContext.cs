using SharedKernel.ValueObjects;

namespace SharedKernel.Application.Interfaces
{
    /// <summary>
    /// Fuente única del contexto actual de ejecución (multitenant / multiempresa).
    /// En este BC usaremos EmpresaId; TenantId queda disponible para otros BCs.
    /// </summary>
    public interface ITenantContext
    {
        TenantId TenantId { get; }
        EmpresaId EmpresaId { get; }
    }
}
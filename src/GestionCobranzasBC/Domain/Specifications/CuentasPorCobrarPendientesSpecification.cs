using GestionCobranzasBC.Domain.Aggregates;
using GestionCobranzasBC.Domain.ValueObjects;
using SharedKernel.Specifications;

namespace GestionCobranzasBC.Domain.Specifications;

/// <summary>
/// Selecciona cuentas por cobrar que aún tienen saldo (pendientes o parciales).
/// </summary>
public sealed class CuentasPorCobrarPendientesSpecification : IBooleanSpecification<CuentaPorCobrar>
{
    public bool IsSatisfiedBy(CuentaPorCobrar candidate)
    {
        if (candidate is null)
        {
            return false;
        }

        var estado = candidate.Estado;

           return estado == EstadoCuentaPorCobrar.Pendiente
               || estado == EstadoCuentaPorCobrar.Parcial;
    }
}

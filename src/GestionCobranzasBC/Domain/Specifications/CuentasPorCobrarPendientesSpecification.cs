using ProyectoFacturaFacil.GestionCobranzasBC.Domain.Aggregates;
using ProyectoFacturaFacil.GestionCobranzasBC.Domain.ValueObjects;
using ProyectoFacturaFacil.SharedKernel.Specifications;

namespace ProyectoFacturaFacil.GestionCobranzasBC.Domain.Specifications;

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

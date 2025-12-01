using System;
using ProyectoFacturaFacil.GestionCobranzasBC.Domain.Aggregates;
using ProyectoFacturaFacil.GestionCobranzasBC.Domain.ValueObjects;
using ProyectoFacturaFacil.SharedKernel.Specifications;

namespace ProyectoFacturaFacil.GestionCobranzasBC.Domain.Specifications;

/// <summary>
/// Selecciona cuentas por cobrar vencidas (tienen saldo y al menos una cuota
/// vencida a la fecha de referencia).
/// </summary>
public sealed class CuentasPorCobrarVencidasSpecification : IBooleanSpecification<CuentaPorCobrar>
{
    public DateOnly FechaReferencia { get; }

    public CuentasPorCobrarVencidasSpecification(DateOnly fechaReferencia)
    {
        FechaReferencia = fechaReferencia;
    }

    public bool IsSatisfiedBy(CuentaPorCobrar candidate)
    {
        if (candidate is null)
        {
            return false;
        }

        if (candidate.Estado == EstadoCuentaPorCobrar.Cancelado)
        {
            return false;
        }

        return candidate.TieneCuotasVencidas(FechaReferencia);
    }
}

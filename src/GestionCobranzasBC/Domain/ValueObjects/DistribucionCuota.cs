using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionCobranzasBC.Domain.ValueObjects;

/// <summary>
/// Representa la asignación de parte de una cobranza a una cuota concreta.
/// </summary>
public sealed record DistribucionCuota
{
    public int NumeroCuota { get; }
    public Dinero Monto { get; }

    private DistribucionCuota(int numeroCuota, Dinero monto)
    {
        NumeroCuota = numeroCuota;
        Monto = monto;
    }

    public static DistribucionCuota Crear(int numeroCuota, Dinero monto)
    {
        if (numeroCuota <= 0)
        {
            throw new BusinessRuleException("El número de cuota debe ser mayor o igual a 1.");
        }

        if (monto is null)
        {
            throw new BusinessRuleException("El monto asignado no puede ser nulo.");
        }

        if (monto.Monto <= 0m)
        {
            throw new BusinessRuleException("El monto asignado a la cuota debe ser mayor a cero.");
        }

        return new DistribucionCuota(numeroCuota, monto);
    }
}

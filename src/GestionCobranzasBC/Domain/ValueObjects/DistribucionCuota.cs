using SharedKernel.Exceptions;

namespace GestionCobranzasBC.Domain.ValueObjects;

/// <summary>
/// Representa la asignación de parte de una cobranza a una cuota concreta.
/// </summary>
public sealed record DistribucionCuota
{
    public int NumeroCuota { get; }
    public decimal Monto { get; }

    private DistribucionCuota(int numeroCuota, decimal monto)
    {
        NumeroCuota = numeroCuota;
        Monto = monto;
    }

    public static DistribucionCuota Crear(int numeroCuota, decimal monto)
    {
        if (numeroCuota <= 0)
        {
            throw new BusinessRuleException("El número de cuota debe ser mayor o igual a 1.");
        }

        if (monto <= 0m)
        {
            throw new BusinessRuleException("El monto asignado a la cuota debe ser mayor a cero.");
        }

        return new DistribucionCuota(numeroCuota, decimal.Round(monto, 2));
    }
}

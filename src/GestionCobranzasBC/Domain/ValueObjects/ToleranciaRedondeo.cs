using SharedKernel.Exceptions;

namespace GestionCobranzasBC.Domain.ValueObjects;

/// <summary>
/// Tolerancia para permitir diferencias mínimas por redondeo
/// entre total, cobrado y saldo.
/// </summary>
public sealed record ToleranciaRedondeo
{
    public decimal Valor { get; }

    private ToleranciaRedondeo(decimal valor)
    {
        Valor = valor;
    }

    public static ToleranciaRedondeo Ninguna => new(0m);

    /// <summary>
    /// Crea una tolerancia entre 0 y 0.05 (5 centimos).
    /// </summary>
    public static ToleranciaRedondeo Desde(decimal valor)
    {
        if (valor < 0m)
        {
            throw new BusinessRuleException("La tolerancia de redondeo no puede ser negativa.");
        }

        if (valor > 0.05m)
        {
            throw new BusinessRuleException("La tolerancia de redondeo no debe ser mayor a 0.05.");
        }

        return new ToleranciaRedondeo(decimal.Round(valor, 4));
    }

    public bool EstaDentro(decimal diferencia)
        => Math.Abs(diferencia) <= Valor;
}

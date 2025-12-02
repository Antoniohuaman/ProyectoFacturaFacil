using System;
using SharedKernel.Exceptions;

namespace GestionCobranzasBC.Domain.ValueObjects;

public readonly record struct CuentaPorCobrarId
{
    public Guid Value { get; }

    private CuentaPorCobrarId(Guid value)
    {
        Value = value;
    }

    public static CuentaPorCobrarId New()
        => new(Guid.NewGuid());

    public static CuentaPorCobrarId From(Guid value)
        => Crear(value);

    public static CuentaPorCobrarId Crear(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new BusinessRuleException("El identificador de la cuenta por cobrar no puede ser vacío.");
        }

        return new CuentaPorCobrarId(value);
    }

    public override string ToString() => Value.ToString();
}

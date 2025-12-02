using System;
using SharedKernel.Exceptions;

namespace GestionCobranzasBC.Domain.ValueObjects;

public readonly record struct CobranzaId
{
    public Guid Value { get; }

    private CobranzaId(Guid value)
    {
        Value = value;
    }

    public static CobranzaId New()
        => new(Guid.NewGuid());

    public static CobranzaId From(Guid value)
        => Crear(value);

    public static CobranzaId Crear(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new BusinessRuleException("El identificador de cobranza no puede ser vacío.");
        }

        return new CobranzaId(value);
    }

    public override string ToString() => Value.ToString();
}

using SharedKernel.Exceptions;

namespace ListaPreciosBC.Domain.ValueObjects;

/// <summary>
/// Configuración de una regla global asociada a una columna de tipo global
/// (descuento o recargo). Solo modela la magnitud; la dirección se determina
/// por el TipoColumnaPrecio (descuento/recargo).
/// </summary>
public sealed class ReglaGlobalColumnaPrecio : IEquatable<ReglaGlobalColumnaPrecio>
{
    public TipoReglaGlobalColumnaPrecio Tipo { get; }
    public decimal Valor { get; }

    private ReglaGlobalColumnaPrecio(TipoReglaGlobalColumnaPrecio tipo, decimal valor)
    {
        Tipo = tipo;
        Valor = valor;
    }

    public static ReglaGlobalColumnaPrecio Crear(
        TipoReglaGlobalColumnaPrecio tipo,
        decimal valor)
    {
        if (valor < 0m)
        {
            throw new BusinessRuleException("El valor de la regla global no puede ser negativo.");
        }

        if (tipo.Equals(TipoReglaGlobalColumnaPrecio.Porcentaje) && valor > 100m)
        {
            throw new BusinessRuleException(
                "El valor de la regla global en porcentaje no puede ser mayor a 100.");
        }

        return new ReglaGlobalColumnaPrecio(tipo, valor);
    }

    /// <summary>
    /// Calcula el ajuste absoluto a aplicar sobre el precio base.
    /// La responsabilidad de aplicar signo (descuento/recargo) recae en el llamador.
    /// </summary>
    public decimal CalcularAjuste(decimal precioBase)
    {
        if (precioBase < 0m)
        {
            throw new BusinessRuleException("El precio base no puede ser negativo.");
        }

        return Tipo.Equals(TipoReglaGlobalColumnaPrecio.Porcentaje)
            ? decimal.Round(precioBase * (Valor / 100m), 2, MidpointRounding.AwayFromZero)
            : Valor;
    }

    public ReglaGlobalColumnaPrecio ConValor(decimal nuevoValor) =>
        Crear(Tipo, nuevoValor);

    #region Equality

    public bool Equals(ReglaGlobalColumnaPrecio? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Tipo.Equals(other.Tipo) && Valor == other.Valor;
    }

    public override bool Equals(object? obj) => Equals(obj as ReglaGlobalColumnaPrecio);

    public override int GetHashCode() => HashCode.Combine(Tipo, Valor);

    #endregion

    public override string ToString() => $"{Tipo.Codigo}:{Valor}";
}

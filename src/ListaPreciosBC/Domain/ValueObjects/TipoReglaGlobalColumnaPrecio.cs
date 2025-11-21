using SharedKernel.Exceptions;

namespace ListaPreciosBC.Domain.ValueObjects;

/// <summary>
/// Tipo de regla global aplicada a una columna global:
/// porcentaje o monto fijo.
/// No define si es descuento o recargo, eso lo determina el TipoColumnaPrecio.
/// </summary>
public sealed class TipoReglaGlobalColumnaPrecio : IEquatable<TipoReglaGlobalColumnaPrecio>
{
    public string Codigo { get; }
    public string Nombre { get; }

    private TipoReglaGlobalColumnaPrecio(string codigo, string nombre)
    {
        Codigo = codigo;
        Nombre = nombre;
    }

    public static readonly TipoReglaGlobalColumnaPrecio Porcentaje =
        new("PORCENTAJE", "Porcentaje");

    public static readonly TipoReglaGlobalColumnaPrecio MontoFijo =
        new("MONTO_FIJO", "Monto fijo");

    private static readonly IReadOnlyDictionary<string, TipoReglaGlobalColumnaPrecio> PorCodigo =
        new Dictionary<string, TipoReglaGlobalColumnaPrecio>(StringComparer.OrdinalIgnoreCase)
        {
            { Porcentaje.Codigo, Porcentaje },
            { MontoFijo.Codigo, MontoFijo }
        };

    public static IEnumerable<TipoReglaGlobalColumnaPrecio> Todos => PorCodigo.Values;

    public static TipoReglaGlobalColumnaPrecio DesdeCodigo(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new BusinessRuleException("El código de tipo de regla global no puede ser vacío.");
        }

        if (!PorCodigo.TryGetValue(codigo.Trim(), out var tipo))
        {
            throw new BusinessRuleException(
                $"El código de tipo de regla global '{codigo}' no es válido.");
        }

        return tipo;
    }

    public override string ToString() => Codigo;

    #region Equality

    public bool Equals(TipoReglaGlobalColumnaPrecio? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Codigo, other.Codigo, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj) => Equals(obj as TipoReglaGlobalColumnaPrecio);

    public override int GetHashCode() =>
        StringComparer.OrdinalIgnoreCase.GetHashCode(Codigo);

    #endregion
}

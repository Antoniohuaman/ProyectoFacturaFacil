using SharedKernel.Exceptions;

namespace ListaPreciosBC.Domain.ValueObjects;

/// <summary>
/// Tipo funcional de la columna de precio, independiente del modo de valorización.
/// Se alinea con el frontend: base, global (descuento/recargo), mínimo permitido, manual.
/// </summary>
public sealed class TipoColumnaPrecio : IEquatable<TipoColumnaPrecio>
{
    public string Codigo { get; }
    public string Nombre { get; }

    private TipoColumnaPrecio(string codigo, string nombre)
    {
        Codigo = codigo;
        Nombre = nombre;
    }

    // Instancias estáticas conocidas
    public static readonly TipoColumnaPrecio Base =
        new("BASE", "Columna base");

    public static readonly TipoColumnaPrecio GlobalDescuento =
        new("GLOBAL_DESCUENTO", "Columna de descuento global");

    public static readonly TipoColumnaPrecio GlobalRecargo =
        new("GLOBAL_RECARGO", "Columna de recargo global");

    public static readonly TipoColumnaPrecio MinimoPermitido =
        new("MINIMO_PERMITIDO", "Columna de precio mínimo permitido");

    public static readonly TipoColumnaPrecio Manual =
        new("MANUAL", "Columna manual");

    private static readonly IReadOnlyDictionary<string, TipoColumnaPrecio> PorCodigo =
        new Dictionary<string, TipoColumnaPrecio>(StringComparer.OrdinalIgnoreCase)
        {
            { Base.Codigo, Base },
            { GlobalDescuento.Codigo, GlobalDescuento },
            { GlobalRecargo.Codigo, GlobalRecargo },
            { MinimoPermitido.Codigo, MinimoPermitido },
            { Manual.Codigo, Manual }
        };

    public static IEnumerable<TipoColumnaPrecio> Todos => PorCodigo.Values;

    public static TipoColumnaPrecio DesdeCodigo(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new BusinessRuleException("El código de tipo de columna de precio no puede ser vacío.");
        }

        if (!PorCodigo.TryGetValue(codigo.Trim(), out var tipo))
        {
            throw new BusinessRuleException(
                $"El código de tipo de columna de precio '{codigo}' no es válido.");
        }

        return tipo;
    }

    public bool EsBase => Equals(Base);

    public bool EsGlobal => Equals(GlobalDescuento) || Equals(GlobalRecargo);

    public bool EsManual => Equals(Manual);

    public bool EsMinimoPermitido => Equals(MinimoPermitido);

    public override string ToString() => Codigo;

    #region Equality

    public bool Equals(TipoColumnaPrecio? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Codigo, other.Codigo, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj) => Equals(obj as TipoColumnaPrecio);

    public override int GetHashCode() =>
        StringComparer.OrdinalIgnoreCase.GetHashCode(Codigo);

    #endregion
}

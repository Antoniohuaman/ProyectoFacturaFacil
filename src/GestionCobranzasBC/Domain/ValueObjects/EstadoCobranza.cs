using SharedKernel.Exceptions;

namespace GestionCobranzasBC.Domain.ValueObjects;

/// <summary>
/// Estado del documento de cobranza (recibo).
/// </summary>
public sealed record EstadoCobranza
{
    public string Codigo { get; }
    public string Descripcion { get; }

    private EstadoCobranza(string codigo, string descripcion)
    {
        Codigo = codigo;
        Descripcion = descripcion;
    }

    public static readonly EstadoCobranza Registrada = new("REG", "Registrada");
    public static readonly EstadoCobranza Anulada    = new("ANU", "Anulada");

    public static EstadoCobranza DesdeCodigo(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new BusinessRuleException("El código de estado de cobranza es obligatorio.");
        }

        return codigo.Trim().ToUpperInvariant() switch
        {
            "REG" => Registrada,
            "ANU" => Anulada,
            _ => throw new BusinessRuleException($"El código de estado de cobranza '{codigo}' no es válido.")
        };
    }

    public bool EstaActiva => this == Registrada;

    public override string ToString() => Descripcion;
}

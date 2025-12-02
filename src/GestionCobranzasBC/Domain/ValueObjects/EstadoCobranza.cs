using SharedKernel.Exceptions;

namespace GestionCobranzasBC.Domain.ValueObjects;

/// <summary>
/// Estado del documento de cobranza (recibo).
/// </summary>
public sealed record EstadoCobranza
{
    private static readonly EstadoCobranza RegistradaState = new("REG", "Registrada");

    public string Codigo { get; }
    public string Descripcion { get; }
    public string? MotivoAnulacion { get; }

    private EstadoCobranza(string codigo, string descripcion, string? motivoAnulacion = null)
    {
        Codigo = codigo;
        Descripcion = descripcion;
        MotivoAnulacion = motivoAnulacion;
    }

    public static EstadoCobranza Registrada() => RegistradaState;

    public static EstadoCobranza Anulada(string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new BusinessRuleException("El motivo de anulación es obligatorio.");
        }

        return new EstadoCobranza("ANU", "Anulada", motivo.Trim());
    }

    public static EstadoCobranza DesdeCodigo(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new BusinessRuleException("El código de estado de cobranza es obligatorio.");
        }

        return codigo.Trim().ToUpperInvariant() switch
        {
            "REG" => Registrada(),
            "ANU" => Anulada("Sin especificar"),
            _ => throw new BusinessRuleException($"El código de estado de cobranza '{codigo}' no es válido.")
        };
    }

    public bool EsAnulada => Codigo == "ANU";
    public bool EstaActiva => !EsAnulada;

    public override string ToString() => Descripcion;
}

using SharedKernel.Exceptions;

namespace GestionCobranzasBC.Domain.ValueObjects;

/// <summary>
/// Estado de una cuenta por cobrar.
/// </summary>
public sealed record EstadoCuentaPorCobrar
{
    private static readonly EstadoCuentaPorCobrar PendienteState = new("PEN", "Pendiente");
    private static readonly EstadoCuentaPorCobrar ParcialState   = new("PAR", "Parcial");
    private static readonly EstadoCuentaPorCobrar VencidaState   = new("VEN", "Vencida");
    private static readonly EstadoCuentaPorCobrar CanceladaState = new("CAN", "Cancelada");

    public string Codigo { get; }
    public string Descripcion { get; }

    private EstadoCuentaPorCobrar(string codigo, string descripcion)
    {
        Codigo = codigo;
        Descripcion = descripcion;
    }

    public static EstadoCuentaPorCobrar Pendiente => PendienteState;
    public static EstadoCuentaPorCobrar Parcial => ParcialState;
    public static EstadoCuentaPorCobrar Vencida => VencidaState;
    public static EstadoCuentaPorCobrar Cancelada => CanceladaState;

    /// <summary>Alias masculino para compatibilidad con código existente.</summary>
    public static EstadoCuentaPorCobrar Vencido => VencidaState;

    /// <summary>Alias masculino para compatibilidad con código existente.</summary>
    public static EstadoCuentaPorCobrar Cancelado => CanceladaState;

    public static EstadoCuentaPorCobrar DesdeCodigo(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new BusinessRuleException("El código de estado de cuenta por cobrar es obligatorio.");
        }

        return codigo.Trim().ToUpperInvariant() switch
        {
            "PEN" => PendienteState,
            "PAR" => ParcialState,
            "VEN" => VencidaState,
            "CAN" => CanceladaState,
            _ => throw new BusinessRuleException($"El código de estado de cuenta por cobrar '{codigo}' no es válido.")
        };
    }

    public bool EsCancelado => Codigo == "CAN";
    public bool EsVencido => Codigo == "VEN";

    public override string ToString() => Descripcion;
}

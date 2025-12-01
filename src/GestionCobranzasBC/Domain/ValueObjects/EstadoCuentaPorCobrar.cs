using SharedKernel.Exceptions;

namespace GestionCobranzasBC.Domain.ValueObjects;

/// <summary>
/// Estado de una cuenta por cobrar.
/// </summary>
public sealed record EstadoCuentaPorCobrar
{
    public string Codigo { get; }
    public string Descripcion { get; }

    private EstadoCuentaPorCobrar(string codigo, string descripcion)
    {
        Codigo = codigo;
        Descripcion = descripcion;
    }

    public static readonly EstadoCuentaPorCobrar Pendiente = new("PEN", "Pendiente");
    public static readonly EstadoCuentaPorCobrar Parcial   = new("PAR", "Parcial");
    public static readonly EstadoCuentaPorCobrar Vencida   = new("VEN", "Vencida");
    public static readonly EstadoCuentaPorCobrar Cancelada = new("CAN", "Cancelada");

    public static EstadoCuentaPorCobrar DesdeCodigo(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new BusinessRuleException("El código de estado de cuenta por cobrar es obligatorio.");
        }

        return codigo.Trim().ToUpperInvariant() switch
        {
            "PEN" => Pendiente,
            "PAR" => Parcial,
            "VEN" => Vencida,
            "CAN" => Cancelada,
            _ => throw new BusinessRuleException($"El código de estado de cuenta por cobrar '{codigo}' no es válido.")
        };
    }

    public override string ToString() => Descripcion;
}

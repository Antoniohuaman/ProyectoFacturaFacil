namespace ComprobantesElectronicosBC.Domain.ValueObjects.Ids;

public readonly record struct UsuarioId(Guid Value)
{
    public static UsuarioId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
    public static implicit operator Guid(UsuarioId id) => id.Value;
}

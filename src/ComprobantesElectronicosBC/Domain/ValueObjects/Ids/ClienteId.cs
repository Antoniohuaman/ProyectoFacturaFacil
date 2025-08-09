namespace ComprobantesElectronicosBC.Domain.ValueObjects.Ids;

public readonly record struct ClienteId(Guid Value)
{
    public static ClienteId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
    public static implicit operator Guid(ClienteId id) => id.Value;
}

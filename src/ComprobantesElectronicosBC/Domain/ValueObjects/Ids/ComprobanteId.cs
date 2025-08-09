namespace ComprobantesElectronicosBC.Domain.ValueObjects.Ids;

public readonly record struct ComprobanteId(Guid Value)
{
    public static ComprobanteId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
    public static implicit operator Guid(ComprobanteId id) => id.Value;
}

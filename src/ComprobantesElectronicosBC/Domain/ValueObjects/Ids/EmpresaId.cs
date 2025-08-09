namespace ComprobantesElectronicosBC.Domain.ValueObjects.Ids;

public readonly record struct EmpresaId(Guid Value)
{
    public static EmpresaId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
    public static implicit operator Guid(EmpresaId id) => id.Value;
}

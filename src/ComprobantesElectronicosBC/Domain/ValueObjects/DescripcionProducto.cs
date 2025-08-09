namespace ComprobantesElectronicosBC.Domain.ValueObjects;

public readonly record struct DescripcionProducto(string Value)
{
    public static DescripcionProducto Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("La descripción no puede estar vacía.");
        var trimmed = value.Trim();
        if (trimmed.Length > 500) throw new ArgumentException("Descripción no debe exceder 500 caracteres.");
        return new(trimmed);
    }

    public override string ToString() => Value;
}

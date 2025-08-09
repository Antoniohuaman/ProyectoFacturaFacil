namespace ComprobantesElectronicosBC.Domain.ValueObjects;

public readonly record struct FechaVencimiento(DateOnly Value)
{
    public static FechaVencimiento Create(DateOnly vencimiento, DateOnly issueDate)
    {
        if (vencimiento < issueDate)
            throw new ArgumentException("La fecha de vencimiento debe ser >= fecha de emisión.");
        return new(vencimiento);
    }

    public override string ToString() => Value.ToString("yyyy-MM-dd");
}

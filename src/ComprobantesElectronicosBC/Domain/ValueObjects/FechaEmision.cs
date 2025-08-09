namespace ComprobantesElectronicosBC.Domain.ValueObjects;

public readonly record struct FechaEmision(DateOnly Value)
{
    public static FechaEmision Today() => new(DateOnly.FromDateTime(DateTime.UtcNow));

    public static FechaEmision Create(DateOnly value, DateTime? clockUtc = null)
    {
        var today = DateOnly.FromDateTime((clockUtc ?? DateTime.UtcNow).Date);
        if (value > today) throw new ArgumentException("La fecha de emisión no puede ser futura.");
        return new(value);
    }

    public override string ToString() => Value.ToString("yyyy-MM-dd");
}

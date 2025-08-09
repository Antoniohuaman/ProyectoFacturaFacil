namespace ComprobantesElectronicosBC.Domain.ValueObjects;

public readonly record struct Cantidad(decimal Value)
{
    public static Cantidad Create(decimal value)
    {
        if (value <= 0m) throw new ArgumentOutOfRangeException(nameof(value), "La cantidad debe ser > 0.");
        // Escala típica para cantidades (ajústalo si lo prefieres):
        if (decimal.Round(value, 6) != value) throw new ArgumentException("Cantidad con más de 6 decimales.");
        return new(value);
    }

    public override string ToString() => Value.ToString("0.######");
}

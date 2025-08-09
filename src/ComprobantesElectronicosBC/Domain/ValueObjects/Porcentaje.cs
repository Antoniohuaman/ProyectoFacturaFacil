namespace ComprobantesElectronicosBC.Domain.ValueObjects;

public readonly record struct Porcentaje(decimal Valor)
{
    public static readonly Porcentaje Cero  = new(0m);
    public static readonly Porcentaje IGV18 = new(18m);

    public static Porcentaje Create(decimal valor)
    {
        if (valor < 0m || valor > 100m) throw new ArgumentOutOfRangeException(nameof(valor), "Porcentaje 0..100.");
        if (decimal.Round(valor, 4) != valor) throw new ArgumentException("Porcentaje con más de 4 decimales.");
        return new(valor);
    }

    public decimal ComoFraccion => Valor / 100m;
    public override string ToString() => $"{Valor:#0.####}%";
}
